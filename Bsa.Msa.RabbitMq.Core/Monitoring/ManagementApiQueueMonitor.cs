using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bsa.Msa.RabbitMq.Core.Settings;

namespace Bsa.Msa.RabbitMq.Core.Monitoring
{
	/// <summary>
	/// Reads all queues of the broker through the management API (GET /api/queues):
	/// messages, ready, unacknowledged, consumers, state and rates.
	/// Requires the rabbitmq_management plugin.
	/// </summary>
	public sealed class ManagementApiQueueMonitor : IQueueMonitor, IDisposable
	{
		private readonly ManagementApiOptions _options;

		private readonly HttpClient _httpClient;

		private readonly bool _ownsHttpClient;

		// Only the needed fields: the full answer for thousands of queues is large.
		private const string Columns = "name,vhost,state,consumers,messages,messages_ready,"
			+ "messages_unacknowledged,message_stats.publish_details.rate,"
			+ "message_stats.deliver_get_details.rate";

		/// <summary>
		/// Creates the monitor from the broker settings, port 15672.
		/// </summary>
		/// <param name="settings">Broker settings.</param>
		public ManagementApiQueueMonitor(IRabbitMqSettings settings)
			: this(ManagementApiOptions.FromSettings(settings))
		{
		}

		/// <summary>
		/// Creates the monitor.
		/// </summary>
		/// <param name="options">Management API connection.</param>
		/// <param name="httpClient">HTTP client; created and disposed by the monitor when <c>null</c>.</param>
		public ManagementApiQueueMonitor(ManagementApiOptions options, HttpClient httpClient = null)
		{
			ArgumentNullException.ThrowIfNull(options);
			if (options.BaseAddress == null)
				throw new ArgumentException("BaseAddress is required.", nameof(options));

			_options = options;
			_ownsHttpClient = httpClient == null;
			_httpClient = httpClient ?? new HttpClient { Timeout = options.Timeout };
		}

		/// <inheritdoc/>
		public async Task<IReadOnlyList<QueueInfo>> GetQueuesAsync(
			QueueQuery query = null,
			CancellationToken cancellationToken = default)
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri());
			var credentials = Encoding.UTF8.GetBytes($"{_options.UserName}:{_options.Password}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

			using var response = await _httpClient
				.SendAsync(request, cancellationToken)
				.ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				throw new InvalidOperationException(
					$"RabbitMQ management API returned {(int)response.StatusCode} {response.ReasonPhrase} for {request.RequestUri}");
			}

			await using var stream = await response.Content
				.ReadAsStreamAsync(cancellationToken)
				.ConfigureAwait(false);
			using var document = await JsonDocument
				.ParseAsync(stream, cancellationToken: cancellationToken)
				.ConfigureAwait(false);
			if (document.RootElement.ValueKind != JsonValueKind.Array)
				throw new InvalidOperationException("RabbitMQ management API returned not an array of queues.");

			return document.RootElement
				.EnumerateArray()
				.Select(ParseQueue)
				.Apply(query);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_ownsHttpClient)
				_httpClient.Dispose();
		}

		/// <summary>
		/// Returns the request address: all queues or queues of the virtual host.
		/// </summary>
		/// <returns>Request address.</returns>
		internal Uri BuildUri()
		{
			// Without the trailing slash a path prefix of the address would be replaced.
			var address = _options.BaseAddress.AbsoluteUri;
			var baseAddress = new Uri(address.EndsWith("/", StringComparison.Ordinal) ? address : address + "/");
			var path = string.IsNullOrEmpty(_options.VirtualHost)
				? "api/queues"
				: $"api/queues/{Uri.EscapeDataString(_options.VirtualHost)}";
			return new Uri(baseAddress, $"{path}?columns={Columns}");
		}

		private static QueueInfo ParseQueue(JsonElement queue)
		{
			return new QueueInfo
			{
				Name = GetString(queue, "name"),
				VirtualHost = GetString(queue, "vhost"),
				State = GetString(queue, "state"),
				Messages = GetLong(queue, "messages") ?? 0,
				MessagesReady = GetLong(queue, "messages_ready") ?? 0,
				MessagesUnacknowledged = GetLong(queue, "messages_unacknowledged") ?? 0,
				Consumers = (int)(GetLong(queue, "consumers") ?? 0),
				PublishRate = GetRate(queue, "publish_details"),
				DeliverRate = GetRate(queue, "deliver_get_details")
			};
		}

		private static string GetString(JsonElement element, string name)
		{
			return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
				? value.GetString()
				: null;
		}

		// Idle queues may have no counters in the answer.
		private static long? GetLong(JsonElement element, string name)
		{
			return element.TryGetProperty(name, out var value)
				&& value.ValueKind == JsonValueKind.Number
				&& value.TryGetInt64(out var number)
					? number
					: (long?)null;
		}

		private static double? GetRate(JsonElement queue, string details)
		{
			if (!queue.TryGetProperty("message_stats", out var stats)
				|| stats.ValueKind != JsonValueKind.Object
				|| !stats.TryGetProperty(details, out var detail)
				|| detail.ValueKind != JsonValueKind.Object
				|| !detail.TryGetProperty("rate", out var rate)
				|| rate.ValueKind != JsonValueKind.Number)
				return null;

			return rate.GetDouble();
		}
	}
}
