using System;
using System.Collections.Generic;
using System.Linq;

namespace Bsa.Msa.Common.Services.Commands.Scheduling
{
	/// <summary>
	/// Runs a command on the given days of month at fixed times.
	/// A negative day counts from the end of the month:
	/// -1 is the last day. A day that a month does not have
	/// (e.g. 31 in April) is skipped in that month.
	/// </summary>
	public sealed class MonthlySchedule : ICommandSchedule
	{
		private readonly int[] _daysOfMonth;

		private readonly TimeSpan[] _times;

		private const int MaxMonthsToScan = 48;

		/// <summary>
		/// Creates a monthly schedule.
		/// </summary>
		/// <param name="daysOfMonth">
		/// Days of month, 1..31 or -31..-1 (from the end).
		/// </param>
		/// <param name="times">Times of day of the runs.</param>
		public MonthlySchedule(
			IEnumerable<int> daysOfMonth,
			IEnumerable<TimeSpan> times)
		{
			_daysOfMonth = ScheduleGuard.GetDaysOfMonth(daysOfMonth);
			_times = ScheduleGuard.GetTimesOfDay(times);
		}

		/// <inheritdoc/>
		public DateTime? GetNextRun(DateTime notBefore)
		{
			var month = new DateTime(
				notBefore.Year,
				notBefore.Month,
				1,
				0,
				0,
				0,
				notBefore.Kind);
			for (var i = 0; i < MaxMonthsToScan; i++)
			{
				var run = GetFirstInMonth(month, notBefore);
				if (run.HasValue) return run;

				month = month.AddMonths(1);
			}

			return null;
		}

		private DateTime? GetFirstInMonth(
			DateTime month,
			DateTime notBefore)
		{
			var length = DateTime.DaysInMonth(month.Year, month.Month);
			var days = _daysOfMonth
				.Select(x => x > 0 ? x : length + x + 1)
				.Where(x => x >= 1 && x <= length)
				.Distinct()
				.OrderBy(x => x);

			foreach (var day in days)
			{
				var date = month.AddDays(day - 1);
				foreach (var time in _times)
				{
					var run = date + time;
					if (run >= notBefore) return run;
				}
			}

			return null;
		}
	}
}
