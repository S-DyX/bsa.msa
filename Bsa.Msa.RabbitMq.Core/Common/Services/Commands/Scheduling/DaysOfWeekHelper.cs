using System;

namespace Bsa.Msa.Common.Services.Commands.Scheduling
{
	/// <summary>
	/// Helpers for the <see cref="DaysOfWeek"/> flags.
	/// </summary>
	internal static class DaysOfWeekHelper
	{
		/// <summary>
		/// Checks whether the date falls on one of the allowed days.
		/// </summary>
		/// <param name="days">
		/// Allowed days; <c>null</c> or <see cref="DaysOfWeek.None"/>
		/// means any day.
		/// </param>
		/// <param name="date">Date to check.</param>
		/// <returns><c>true</c> if the day is allowed.</returns>
		public static bool IsAllowed(DaysOfWeek? days, DateTime date)
		{
			if (!days.HasValue || days.Value == DaysOfWeek.None) return true;

			return (days.Value & ToFlag(date.DayOfWeek)) != 0;
		}

		private static DaysOfWeek ToFlag(DayOfWeek day)
		{
			switch (day)
			{
				case DayOfWeek.Monday:
					return DaysOfWeek.Monday;
				case DayOfWeek.Tuesday:
					return DaysOfWeek.Tuesday;
				case DayOfWeek.Wednesday:
					return DaysOfWeek.Wednesday;
				case DayOfWeek.Thursday:
					return DaysOfWeek.Thursday;
				case DayOfWeek.Friday:
					return DaysOfWeek.Friday;
				case DayOfWeek.Saturday:
					return DaysOfWeek.Saturday;
				default:
					return DaysOfWeek.Sunday;
			}
		}
	}
}
