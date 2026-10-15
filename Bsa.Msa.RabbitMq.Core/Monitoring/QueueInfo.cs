namespace Bsa.Msa.RabbitMq.Core.Monitoring
{
	/// <summary>
	/// State of a queue at the moment of the request.
	/// </summary>
	public sealed class QueueInfo
	{
		/// <summary>
		/// Queue name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Virtual host.
		/// </summary>
		public string VirtualHost { get; set; }

		/// <summary>
		/// All messages: ready and unacknowledged.
		/// </summary>
		public long Messages { get; set; }

		/// <summary>
		/// Messages waiting for a consumer.
		/// </summary>
		public long MessagesReady { get; set; }

		/// <summary>
		/// Messages delivered but not acknowledged yet;
		/// <c>null</c> if the monitor can not get it (AMQP).
		/// </summary>
		public long? MessagesUnacknowledged { get; set; }

		/// <summary>
		/// Number of consumers.
		/// </summary>
		public int Consumers { get; set; }

		/// <summary>
		/// Queue state, e.g. "running"; <c>null</c> if unknown.
		/// </summary>
		public string State { get; set; }

		/// <summary>
		/// Messages published per second; <c>null</c> if unknown.
		/// </summary>
		public double? PublishRate { get; set; }

		/// <summary>
		/// Messages delivered per second; <c>null</c> if unknown.
		/// </summary>
		public double? DeliverRate { get; set; }
	}
}
