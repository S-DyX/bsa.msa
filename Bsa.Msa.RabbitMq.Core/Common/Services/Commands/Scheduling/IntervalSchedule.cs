using System;

namespace Bsa.Msa.Common.Services.Commands.Scheduling
{
	/// <summary>
	/// Runs a command every period.
	/// Without a window the runs follow one grid that starts
	/// at the anchor; with a window (<c>from</c> - <c>to</c>)
	/// the grid restarts at the window start every day.
	/// </summary>
	public sealed class IntervalSchedule : ICommandSchedule
	{
		private readonly TimeSpan _period;

		private readonly TimeSpan _start;

		private readonly TimeSpan? _from;

		private readonly TimeSpan? _to;

		private readonly DaysOfWeek? _days;

		private DateTime? _anchor;

		private const int MaxDaysToScan = 64;

		/// <summary>
		/// Creates an interval schedule.
		/// </summary>
		/// <param name="period">Interval between runs.</param>
		/// <param name="start">
		/// Time of day of the first run when there is no window.
		/// Zero means "start immediately".
		/// </param>
		/// <param name="from">Daily window start, time of day.</param>
		/// <param name="to">
		/// Daily window end, time of day. Not later than
		/// <paramref name="from"/> means the next day;
		/// <c>null</c> means 24 hours after the window start.
		/// </param>
		/// <param name="days">
		/// Allowed days of week. For a window it is the day
		/// the window starts on.
		/// </param>
		public IntervalSchedule(
			TimeSpan period,
			TimeSpan start,
			TimeSpan? from = null,
			TimeSpan? to = null,
			DaysOfWeek? days = null)
		{
			if (period <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(
					nameof(period),
					"Period must be positive.");
			}

			_period = period;
			_start = start;
			_from = from;
			_to = to;
			_days = days;
		}

		/// <inheritdoc/>
		public DateTime? GetNextRun(DateTime notBefore)
		{
			if (_from.HasValue) return GetNextInWindow(notBefore);

			// The grid is anchored at the first call (the repeater start),
			// so "no start time" keeps meaning "run immediately".
			_anchor ??= GetAnchor(notBefore);
			return GetNextOnGrid(_anchor.Value, notBefore);
		}

		private DateTime GetAnchor(DateTime now)
		{
			if (_start <= TimeSpan.Zero) return now;

			var anchor = now.Date + _start;
			return anchor >= now ? anchor : anchor.AddDays(1);
		}

		private DateTime? GetNextOnGrid(
			DateTime anchor,
			DateTime notBefore)
		{
			var candidate = Align(anchor, notBefore);
			for (var i = 0; i < MaxDaysToScan; i++)
			{
				if (DaysOfWeekHelper.IsAllowed(_days, candidate))
					return candidate;

				// Skip the rest of a forbidden day in one step.
				var nextDay = candidate.Date.AddDays(1);
				candidate = Align(anchor, nextDay);
			}

			return null;
		}

		private DateTime? GetNextInWindow(DateTime notBefore)
		{
			// A window crossing midnight may have started yesterday.
			var day = notBefore.Date.AddDays(-1);
			for (var i = 0; i < MaxDaysToScan; i++)
			{
				if (DaysOfWeekHelper.IsAllowed(_days, day))
				{
					var start = day + _from.Value;
					var end = GetWindowEnd(day, start);
					var candidate = Align(start, notBefore);
					if (candidate < end) return candidate;
				}

				day = day.AddDays(1);
			}

			return null;
		}

		private DateTime GetWindowEnd(DateTime day, DateTime start)
		{
			if (!_to.HasValue) return start.AddDays(1);

			var end = day + _to.Value;
			return end > start ? end : end.AddDays(1);
		}

		private DateTime Align(DateTime start, DateTime notBefore)
		{
			if (notBefore <= start) return start;

			var periods = (notBefore - start).Ticks / _period.Ticks;
			var candidate = start.AddTicks(periods * _period.Ticks);
			return candidate < notBefore
				? candidate.Add(_period)
				: candidate;
		}
	}
}
