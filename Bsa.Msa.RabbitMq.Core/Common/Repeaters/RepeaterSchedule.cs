using System;
using Bsa.Msa.Common.Services.Commands;

namespace Bsa.Msa.Common.Repeaters
{
	/// <summary>
	/// Schedule helpers shared by the repeaters.
	/// </summary>
	internal static class RepeaterSchedule
	{
		private static readonly TimeSpan Day = TimeSpan.FromDays(1);

		/// <summary>
		/// Returns the delay before the first run.
		/// A positive <paramref name="startTime"/> is a local time of day: the first run
		/// happens at the nearest such time. Zero means "start immediately".
		/// </summary>
		/// <param name="startTime">Local time of day of the first run.</param>
		/// <returns>Delay before the first run.</returns>
		public static TimeSpan GetStartDelay(TimeSpan startTime)
		{
			if (startTime <= TimeSpan.Zero) return TimeSpan.Zero;

			var now = DateTime.Now.TimeOfDay;
			var delay = startTime - now;
			return delay >= TimeSpan.Zero ? delay : delay + Day;
		}

		/// <summary>
		/// Checks whether the command may run today.
		/// </summary>
		/// <param name="days">Allowed days; <c>null</c> or <see cref="DaysOfWeek.None"/> means any day.</param>
		/// <returns><c>true</c> if today is an allowed day.</returns>
		public static bool IsAllowedToday(DaysOfWeek? days)
		{
			if (!days.HasValue || days.Value == DaysOfWeek.None) return true;

			return (days.Value & ToFlag(DateTime.Now.DayOfWeek)) != 0;
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
