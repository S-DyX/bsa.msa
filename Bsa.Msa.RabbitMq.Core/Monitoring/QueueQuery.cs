namespace Bsa.Msa.RabbitMq.Core.Monitoring
{
	/// <summary>
	/// Filter and order of the monitored queues.
	/// By default all queues, the largest first.
	/// </summary>
	public sealed class QueueQuery
	{
		/// <summary>
		/// Sort field; <see cref="QueueSortField.Messages"/> by default.
		/// </summary>
		public QueueSortField SortBy { get; set; } = QueueSortField.Messages;

		/// <summary>
		/// Descending order (the largest first); <c>true</c> by default.
		/// </summary>
		public bool Descending { get; set; } = true;

		/// <summary>
		/// Only queues whose name contains this text (case-insensitive).
		/// </summary>
		public string NameContains { get; set; }

		/// <summary>
		/// Only queues with at least this number of messages.
		/// </summary>
		public long? MinMessages { get; set; }

		/// <summary>
		/// Maximum number of returned queues.
		/// </summary>
		public int? Top { get; set; }
	}
}
