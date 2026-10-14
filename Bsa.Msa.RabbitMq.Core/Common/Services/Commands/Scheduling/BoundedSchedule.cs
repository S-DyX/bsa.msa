using System;

namespace Bsa.Msa.Common.Services.Commands.Scheduling
{
	/// <summary>
	/// Limits another schedule to a date range
	/// (<c>startDate</c> - <c>endDate</c>).
	/// </summary>
	public sealed class BoundedSchedule : ICommandSchedule
	{
		private readonly ICommandSchedule _schedule;

		private readonly DateTime? _startDate;

		private readonly DateTime? _endDate;

		/// <summary>
		/// Creates a bounded schedule.
		/// </summary>
		/// <param name="schedule">Inner schedule.</param>
		/// <param name="startDate">No runs before this moment.</param>
		/// <param name="endDate">No runs after this moment.</param>
		public BoundedSchedule(
			ICommandSchedule schedule,
			DateTime? startDate,
			DateTime? endDate)
		{
			ArgumentNullException.ThrowIfNull(schedule);

			_schedule = schedule;
			_startDate = startDate;
			_endDate = endDate;
		}

		/// <inheritdoc/>
		public DateTime? GetNextRun(DateTime notBefore)
		{
			if (_startDate.HasValue && notBefore < _startDate.Value)
				notBefore = _startDate.Value;

			var run = _schedule.GetNextRun(notBefore);
			if (run.HasValue && _endDate.HasValue && run > _endDate)
				return null;

			return run;
		}
	}
}
