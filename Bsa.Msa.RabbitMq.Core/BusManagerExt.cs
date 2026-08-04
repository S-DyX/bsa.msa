using Bsa.Msa.Common;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;

namespace Bsa.Msa.RabbitMq.Core
{
	/// <summary>
	/// Provides extension methods for creating an <see cref="IBusManager"/> instance from various sources.
	/// </summary>
	public static class BusManagerExt
	{
		/// <summary>
		/// Creates a new <see cref="IBusManager"/> using the provided RabbitMQ settings.
		/// </summary>
		/// <param name="settings">The RabbitMQ connection settings (host, port, credentials, etc.).</param>
		/// <param name="logger">Optional logger for capturing internal events and errors.</param>
		/// <param name="serializeService">Optional serialization service for message payloads; if <c>null</c>, a default implementation may be used.</param>
		/// <param name="busNaming">Optional naming strategy for queues, exchanges, and routing keys.</param>
		/// <returns>A fully configured <see cref="IBusManager"/> instance ready for use.</returns>
		public static IBusManager CreateBus(this IRabbitMqSettings settings, ILocalLogger logger = null, ISerializeService serializeService = null, ISimpleBusNaming busNaming = null)
		{
			var connection = new SimpleConnection(settings);
			return new BusManager(new SimpleBus(connection, logger, serializeService, busNaming));
		}

		/// <summary>
		/// Creates a new <see cref="IBusManager"/> using a connection string.
		/// </summary>
		/// <param name="connection">The connection string containing host, port, virtual host, and credentials.</param>
		/// <param name="logger">Optional logger for capturing internal events and errors.</param>
		/// <param name="serializeService">Optional serialization service for message payloads; if <c>null</c>, a default implementation may be used.</param>
		/// <param name="busNaming">Optional naming strategy for queues, exchanges, and routing keys.</param>
		/// <returns>A fully configured <see cref="IBusManager"/> instance ready for use.</returns>
		/// <remarks>
		/// This method internally constructs a <see cref="RabbitMqSettings"/> object from the connection string
		/// and delegates to the overload that accepts <see cref="IRabbitMqSettings"/>.
		/// </remarks>
		public static IBusManager CreateBus(this string connection, ILocalLogger logger = null, ISerializeService serializeService = null, ISimpleBusNaming busNaming = null)
		{
			return CreateBus(new RabbitMqSettings(connection), logger, serializeService, busNaming);
		}
	}

}
