using RabbitMQ.Client;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes
{
	/// <summary>
	/// A message published to the mocked channel.
	/// </summary>
	public sealed class PublishedMessage
	{
		/// <summary>
		/// Creates the record.
		/// </summary>
		public PublishedMessage(string exchange, string routingKey, IBasicProperties properties, string body)
		{
			Exchange = exchange;
			RoutingKey = routingKey;
			Properties = properties;
			Body = body;
		}

		/// <summary>
		/// Exchange.
		/// </summary>
		public string Exchange { get; }

		/// <summary>
		/// Routing key (the queue for the default exchange).
		/// </summary>
		public string RoutingKey { get; }

		/// <summary>
		/// Message properties.
		/// </summary>
		public IBasicProperties Properties { get; }

		/// <summary>
		/// Message body.
		/// </summary>
		public string Body { get; }
	}
}
