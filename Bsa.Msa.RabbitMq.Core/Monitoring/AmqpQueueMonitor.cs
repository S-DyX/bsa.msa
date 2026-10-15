using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bsa.Msa.Common;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Bsa.Msa.RabbitMq.Core.Monitoring
{
	/// <summary>
	/// Reads the given queues over AMQP (passive declare), without the
	/// management plugin. AMQP can not list queues and returns only ready
	/// messages: <see cref="QueueInfo.MessagesUnacknowledged"/> is <c>null</c>,
	/// rates are not available. Missing queues are skipped.
	/// </summary>
	public sealed class AmqpQueueMonitor : IQueueMonitor, IDisposable
	{
		private readonly ISimpleConnection _connection;

		private readonly Func<IEnumerable<string>> _getQueueNames;

		private readonly string _virtualHost;

		private readonly bool _ownsConnection;

		private const string ErrorSuffix = ".Error";

		private const string RetrySuffix = ".Retry";

		/// <summary>
		/// Creates the monitor with its own connection: for a missing queue the
		/// broker closes the channel, which must not disturb the message bus.
		/// </summary>
		/// <param name="settings">Broker settings.</param>
		/// <param name="getQueueNames">Returns the queues to read on every request.</param>
		/// <param name="logger">Logger.</param>
		public AmqpQueueMonitor(
			IRabbitMqSettings settings,
			Func<IEnumerable<string>> getQueueNames,
			ILocalLogger logger = null)
			: this(new SimpleConnection(settings, logger), getQueueNames, settings?.VirtualHost, true)
		{
		}

		/// <summary>
		/// Creates the monitor on an existing connection.
		/// </summary>
		/// <param name="connection">Connection.</param>
		/// <param name="getQueueNames">Returns the queues to read on every request.</param>
		/// <param name="virtualHost">Virtual host for <see cref="QueueInfo.VirtualHost"/>.</param>
		/// <param name="ownsConnection">Dispose the connection with the monitor.</param>
		internal AmqpQueueMonitor(
			ISimpleConnection connection,
			Func<IEnumerable<string>> getQueueNames,
			string virtualHost,
			bool ownsConnection)
		{
			ArgumentNullException.ThrowIfNull(connection);
			ArgumentNullException.ThrowIfNull(getQueueNames);

			_connection = connection;
			_getQueueNames = getQueueNames;
			_virtualHost = virtualHost;
			_ownsConnection = ownsConnection;
		}

		/// <summary>
		/// Adds the error and retry queues of every handler queue.
		/// </summary>
		/// <param name="queues">Handler queues.</param>
		/// <returns>Queues with their ".Error" and ".Retry" queues.</returns>
		public static IEnumerable<string> WithServiceQueues(IEnumerable<string> queues)
		{
			return (queues ?? Enumerable.Empty<string>())
				.Where(x => !string.IsNullOrEmpty(x))
				.SelectMany(x => new[] { x, x + ErrorSuffix, x + RetrySuffix });
		}

		/// <inheritdoc/>
		public Task<IReadOnlyList<QueueInfo>> GetQueuesAsync(
			QueueQuery query = null,
			CancellationToken cancellationToken = default)
		{
			var names = (_getQueueNames() ?? Enumerable.Empty<string>())
				.Where(x => !string.IsNullOrEmpty(x))
				.Distinct(StringComparer.Ordinal)
				.ToList();
			var queues = new List<QueueInfo>(names.Count);
			foreach (var name in names)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var queue = TryGetQueue(name);
				if (queue != null)
					queues.Add(queue);
			}

			IReadOnlyList<QueueInfo> result = queues.Apply(query);
			return Task.FromResult(result);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_ownsConnection)
				_connection.Dispose();
		}

		private QueueInfo TryGetQueue(string name)
		{
			try
			{
				QueueDeclareOk result = null;
				_connection.Execute(getChannel => result = getChannel().QueueDeclarePassive(name));
				return new QueueInfo
				{
					Name = name,
					VirtualHost = _virtualHost,
					Messages = result.MessageCount,
					MessagesReady = result.MessageCount,
					Consumers = (int)result.ConsumerCount
				};
			}
			catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
			{
				// No such queue; the connection opens a new channel on the next call.
				return null;
			}
		}
	}
}
