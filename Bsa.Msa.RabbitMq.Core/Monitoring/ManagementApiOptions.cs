using System;
using Bsa.Msa.RabbitMq.Core.Settings;

namespace Bsa.Msa.RabbitMq.Core.Monitoring
{
	/// <summary>
	/// Connection to the RabbitMQ management API (management plugin).
	/// </summary>
	public sealed class ManagementApiOptions
	{
		/// <summary>
		/// Default port of the management plugin.
		/// </summary>
		public const int DefaultPort = 15672;

		/// <summary>
		/// API address, e.g. http://rabbit:15672/.
		/// </summary>
		public Uri BaseAddress { get; set; }

		/// <summary>
		/// User with the "monitoring" tag at least.
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// Password.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Virtual host; <c>null</c> means queues of all virtual hosts.
		/// </summary>
		public string VirtualHost { get; set; }

		/// <summary>
		/// Request timeout.
		/// </summary>
		public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

		/// <summary>
		/// Builds the options from the broker settings (the "rabbitMq" section).
		/// </summary>
		/// <param name="settings">Broker settings.</param>
		/// <param name="port">Management API port.</param>
		/// <param name="scheme">"http" or "https".</param>
		/// <returns>The options.</returns>
		public static ManagementApiOptions FromSettings(
			IRabbitMqSettings settings,
			int port = DefaultPort,
			string scheme = "http")
		{
			ArgumentNullException.ThrowIfNull(settings);

			return new ManagementApiOptions
			{
				BaseAddress = new UriBuilder(scheme, settings.Host, port).Uri,
				UserName = settings.UserName,
				Password = settings.Password,
				VirtualHost = settings.VirtualHost
			};
		}
	}
}
