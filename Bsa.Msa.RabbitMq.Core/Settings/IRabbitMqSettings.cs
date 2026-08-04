using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.RabbitMq.Core.Settings
{
	/// <summary>
	/// Represents the configuration settings required to establish a connection to a RabbitMQ message broker.
	/// </summary>
	public interface IRabbitMqSettings : ISettings
	{
		/// <summary>
		/// Gets a friendly name for the connection, used for identification and logging purposes.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the username used to authenticate with the RabbitMQ server.
		/// </summary>
		string UserName { get; }

		/// <summary>
		/// Gets the password used to authenticate with the RabbitMQ server.
		/// </summary>
		string Password { get; }

		/// <summary>
		/// Gets the hostname or IP address of the RabbitMQ server.
		/// </summary>
		string Host { get; }

		/// <summary>
		/// Gets the virtual host to connect to within the RabbitMQ server.
		/// If not specified, the default virtual host ("/") is typically used.
		/// </summary>
		string VirtualHost { get; }
	}
}
