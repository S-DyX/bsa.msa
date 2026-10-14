using System;
using System.Globalization;
using System.Linq;

namespace Bsa.Msa.Common.Services.Commands.Scheduling
{
	/// <summary>
	/// Builds a <see cref="ICommandSchedule"/> from command settings.
	/// </summary>
	/// <remarks>
	/// Keys: <c>schedule</c> (interval | daily | weekly | monthly,
	/// interval by default), <c>period</c>, <c>start</c>,
	/// <c>from</c>, <c>to</c>, <c>at</c>, <c>days</c>,
	/// <c>daysOfMonth</c>, <c>startDate</c>, <c>endDate</c>.
	/// Lists are separated by <c>;</c> or <c>,</c>.
	/// </remarks>
	internal static class CommandScheduleParser
	{
		private static readonly char[] Separators = { ';', ',' };

		/// <summary>
		/// Parses the schedule of a command.
		/// </summary>
		/// <param name="getValue">Returns a setting value by key.</param>
		/// <param name="commandName">Command name for error messages.</param>
		/// <returns>The schedule.</returns>
		public static ICommandSchedule Parse(
			Func<string, string> getValue,
			string commandName)
		{
			try
			{
				var schedule = ParseSchedule(getValue);
				var startDate = ParseDate(getValue("startDate"));
				var endDate = ParseDate(getValue("endDate"));
				if (!startDate.HasValue && !endDate.HasValue)
					return schedule;

				return new BoundedSchedule(schedule, startDate, endDate);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(
					$"Invalid schedule of command '{commandName}': {ex.Message}",
					ex);
			}
		}

		/// <summary>
		/// Parses <see cref="DaysOfWeek"/> flags, e.g. "Monday, Friday".
		/// </summary>
		/// <param name="value">Setting value.</param>
		/// <returns>Days or <c>null</c> if the value is empty.</returns>
		public static DaysOfWeek? ParseDays(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			var normalized = value.Replace(';', ',');
			return Enum.Parse<DaysOfWeek>(normalized, true);
		}

		private static ICommandSchedule ParseSchedule(
			Func<string, string> getValue)
		{
			var days = ParseDays(getValue("days"));
			switch (ParseKind(getValue("schedule")))
			{
				case ScheduleKind.Daily:
					return new DailySchedule(ParseAt(getValue), days);
				case ScheduleKind.Weekly:
					if (!days.HasValue || days == DaysOfWeek.None)
						throw new FormatException("'days' is required.");
					return new DailySchedule(ParseAt(getValue), days);
				case ScheduleKind.Monthly:
					return new MonthlySchedule(
						ParseDaysOfMonth(getValue("daysOfMonth")),
						ParseAt(getValue));
				default:
					return ParseInterval(getValue, days);
			}
		}

		private static ICommandSchedule ParseInterval(
			Func<string, string> getValue,
			DaysOfWeek? days)
		{
			var period = ParseTime(getValue("period"))
				?? throw new FormatException("'period' is required.");

			return new IntervalSchedule(
				period,
				ParseTime(getValue("start")) ?? TimeSpan.Zero,
				ParseTime(getValue("from")),
				ParseTime(getValue("to")),
				days);
		}

		private static ScheduleKind ParseKind(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return ScheduleKind.Interval;

			return Enum.Parse<ScheduleKind>(value.Trim(), true);
		}

		private static TimeSpan[] ParseAt(Func<string, string> getValue)
		{
			// "start" is accepted as an alias of "at".
			var value = getValue("at");
			if (string.IsNullOrWhiteSpace(value))
				value = getValue("start");
			if (string.IsNullOrWhiteSpace(value))
				throw new FormatException("'at' is required.");

			return Split(value)
				.Select(x => ParseTime(x).Value)
				.ToArray();
		}

		private static int[] ParseDaysOfMonth(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new FormatException("'daysOfMonth' is required.");

			return Split(value)
				.Select(x => int.Parse(x, CultureInfo.InvariantCulture))
				.ToArray();
		}

		private static TimeSpan? ParseTime(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			return TimeSpan.Parse(value.Trim(), CultureInfo.InvariantCulture);
		}

		private static DateTime? ParseDate(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return null;

			return DateTime.Parse(
				value.Trim(),
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeLocal);
		}

		private static string[] Split(string value)
		{
			return value
				.Split(Separators, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.Trim())
				.Where(x => x.Length > 0)
				.ToArray();
		}
	}
}
