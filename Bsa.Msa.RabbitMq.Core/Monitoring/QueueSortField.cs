namespace Bsa.Msa.RabbitMq.Core.Monitoring
{
	/// <summary>
	/// Field to sort queues by.
	/// </summary>
	public enum QueueSortField
	{
		/// <summary>
		/// All messages: ready and unacknowledged.
		/// </summary>
		Messages,

		/// <summary>
		/// Messages waiting for a consumer.
		/// </summary>
		MessagesReady,

		/// <summary>
		/// Messages delivered but not acknowledged yet.
		/// </summary>
		MessagesUnacknowledged,

		/// <summary>
		/// Number of consumers.
		/// </summary>
		Consumers,

		/// <summary>
		/// Messages published per second (management API only).
		/// </summary>
		PublishRate,

		/// <summary>
		/// Messages delivered per second (management API only).
		/// </summary>
		DeliverRate,

		/// <summary>
		/// Queue name.
		/// </summary>
		Name
	}
}
