using System;
using Bsa.Msa.Common.Services.Commands.Scheduling;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes
{
	/// <summary>
	/// Schedule defined by a delegate.
	/// </summary>
	public sealed class DelegateSchedule : ICommandSchedule
	{
		private readonly Func<DateTime, DateTime?> _getNextRun;

		/// <summary>
		/// Creates the schedule.
		/// </summary>
		/// <param name="getNextRun">Returns the next run time.</param>
		public DelegateSchedule(Func<DateTime, DateTime?> getNextRun)
		{
			_getNextRun = getNextRun;
		}

		/// <inheritdoc/>
		public DateTime? GetNextRun(DateTime notBefore)
		{
			return _getNextRun(notBefore);
		}
	}
}
