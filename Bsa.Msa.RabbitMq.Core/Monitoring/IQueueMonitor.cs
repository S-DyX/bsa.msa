using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.RabbitMq.Core.Monitoring
{
	/// <summary>
	/// Reads the number of messages in the queues.
	/// </summary>
	public interface IQueueMonitor
	{
		/// <summary>
		/// Returns the queues filtered and sorted by <paramref name="query"/>.
		/// </summary>
		/// <param name="query">Filter and order; <c>null</c> means all queues, the largest first.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>Queue states.</returns>
		Task<IReadOnlyList<QueueInfo>> GetQueuesAsync(
			QueueQuery query = null,
			CancellationToken cancellationToken = default);
	}
}
