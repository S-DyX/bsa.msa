using System;
using System.Collections.Generic;
using System.Linq;

namespace Bsa.Msa.Common.Services.Commands.Scheduling
{
	/// <summary>
	/// Runs a command at fixed times of day, every day
	/// or only on the given days of week (weekly schedule).
	/// </summary>
	public sealed class DailySchedule : ICommandSchedule
	{
		private readonly TimeSpan[] _times;

		private readonly DaysOfWeek? _days;

		private const int MaxDaysToScan = 8;

		/// <summary>
		/// Creates a daily or weekly schedule.
		/// </summary>
		/// <param name="times">Times of day of the runs.</param>
		/// <param name="days">
		/// Allowed days of week; <c>null</c> means every day.
		/// </param>
		public DailySchedule(
			IEnumerable<TimeSpan> times,
			DaysOfWeek? days = null)
		{
			_times = ScheduleGuard.GetTimesOfDay(times);
			_days = days;
		}

		/// <inheritdoc/>
		public DateTime? GetNextRun(DateTime notBefore)
		{
			var day = notBefore.Date;
			for (var i = 0; i < MaxDaysToScan; i++)
			{
				if (DaysOfWeekHelper.IsAllowed(_days, day))
				{
					var run = _times
						.Select(x => day + x)
						.FirstOrDefault(x => x >= notBefore);
					if (run != default) return run;
				}

				day = day.AddDays(1);
			}

			return null;
		}
	}
}
