using System;
using System.Collections.Generic;
using System.Linq;

namespace Bsa.Msa.Common.Services.Commands.Scheduling
{
	/// <summary>
	/// Validates and normalizes schedule arguments.
	/// </summary>
	internal static class ScheduleGuard
	{
		private static readonly TimeSpan Day = TimeSpan.FromDays(1);

		/// <summary>
		/// Returns distinct sorted times of day.
		/// </summary>
		/// <param name="times">Times of day.</param>
		/// <returns>Sorted times.</returns>
		public static TimeSpan[] GetTimesOfDay(IEnumerable<TimeSpan> times)
		{
			ArgumentNullException.ThrowIfNull(times);

			var result = times.Distinct().OrderBy(x => x).ToArray();
			if (result.Length == 0)
			{
				throw new ArgumentException(
					"At least one time of day is required.",
					nameof(times));
			}

			if (result.Any(x => x < TimeSpan.Zero || x >= Day))
			{
				throw new ArgumentOutOfRangeException(
					nameof(times),
					"Time of day must be within 00:00 - 23:59:59.");
			}

			return result;
		}

		/// <summary>
		/// Returns distinct days of month.
		/// </summary>
		/// <param name="days">Days, 1..31 or -31..-1.</param>
		/// <returns>Validated days.</returns>
		public static int[] GetDaysOfMonth(IEnumerable<int> days)
		{
			ArgumentNullException.ThrowIfNull(days);

			var result = days.Distinct().ToArray();
			if (result.Length == 0)
			{
				throw new ArgumentException(
					"At least one day of month is required.",
					nameof(days));
			}

			if (result.Any(x => x == 0 || x < -31 || x > 31))
			{
				throw new ArgumentOutOfRangeException(
					nameof(days),
					"Day of month must be 1..31 or -31..-1.");
			}

			return result;
		}
	}
}
