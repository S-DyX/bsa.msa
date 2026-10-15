using System;
using System.Collections.Generic;
using System.Linq;

namespace Bsa.Msa.RabbitMq.Core.Monitoring
{
	/// <summary>
	/// Applies <see cref="QueueQuery"/> to a list of queues.
	/// </summary>
	public static class QueueQueryExtensions
	{
		/// <summary>
		/// Filters, sorts and limits the queues.
		/// Queues with equal values are ordered by name.
		/// </summary>
		/// <param name="queues">Queues.</param>
		/// <param name="query">Query; <c>null</c> means the default query.</param>
		/// <returns>Resulting queues.</returns>
		public static List<QueueInfo> Apply(this IEnumerable<QueueInfo> queues, QueueQuery query)
		{
			ArgumentNullException.ThrowIfNull(queues);

			query ??= new QueueQuery();
			var result = queues.Where(x => x != null);
			if (!string.IsNullOrEmpty(query.NameContains))
			{
				result = result.Where(x => x.Name != null
					&& x.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase));
			}

			if (query.MinMessages.HasValue)
				result = result.Where(x => x.Messages >= query.MinMessages.Value);

			var sorted = Sort(result, query).ThenBy(x => x.Name, StringComparer.Ordinal);
			return query.Top.HasValue
				? sorted.Take(Math.Max(0, query.Top.Value)).ToList()
				: sorted.ToList();
		}

		private static IOrderedEnumerable<QueueInfo> Sort(IEnumerable<QueueInfo> queues, QueueQuery query)
		{
			if (query.SortBy == QueueSortField.Name)
			{
				return query.Descending
					? queues.OrderByDescending(x => x.Name, StringComparer.Ordinal)
					: queues.OrderBy(x => x.Name, StringComparer.Ordinal);
			}

			Func<QueueInfo, double> key = GetKey(query.SortBy);
			return query.Descending ? queues.OrderByDescending(key) : queues.OrderBy(key);
		}

		private static Func<QueueInfo, double> GetKey(QueueSortField field)
		{
			// Unknown values (null) go last in the descending order.
			switch (field)
			{
				case QueueSortField.MessagesReady:
					return x => x.MessagesReady;
				case QueueSortField.MessagesUnacknowledged:
					return x => x.MessagesUnacknowledged ?? -1;
				case QueueSortField.Consumers:
					return x => x.Consumers;
				case QueueSortField.PublishRate:
					return x => x.PublishRate ?? -1;
				case QueueSortField.DeliverRate:
					return x => x.DeliverRate ?? -1;
				default:
					return x => x.Messages;
			}
		}
	}
}
