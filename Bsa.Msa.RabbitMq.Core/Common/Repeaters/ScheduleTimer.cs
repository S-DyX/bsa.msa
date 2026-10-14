using System;
using System.Threading;
using Bsa.Msa.Common.Services.Commands.Scheduling;

namespace Bsa.Msa.Common.Repeaters
{
	/// <summary>
	/// One-shot timer that follows an <see cref="ICommandSchedule"/>.
	/// Long delays are split into short steps, so changes of the
	/// system clock, DST and sleep mode do not shift the runs.
	/// </summary>
	internal sealed class ScheduleTimer : IDisposable
	{
		private readonly ICommandSchedule _schedule;

		private readonly Action _onDue;

		private readonly ILocalLogger _logger;

		private readonly string _name;

		private readonly object _sync = new object();

		private readonly Timer _timer;

		private DateTime? _nextRun;

		private bool _isDisposed;

		private static readonly TimeSpan MaxStep = TimeSpan.FromMinutes(1);

		/// <summary>
		/// Creates a timer; call <see cref="Start"/> to arm it.
		/// </summary>
		/// <param name="schedule">Schedule of the runs.</param>
		/// <param name="onDue">Called on every run.</param>
		/// <param name="logger">Logger.</param>
		/// <param name="name">Command name for the log.</param>
		public ScheduleTimer(
			ICommandSchedule schedule,
			Action onDue,
			ILocalLogger logger,
			string name)
		{
			ArgumentNullException.ThrowIfNull(schedule);
			ArgumentNullException.ThrowIfNull(onDue);

			_schedule = schedule;
			_onDue = onDue;
			_logger = logger;
			_name = name;
			_timer = new Timer(
				HandleTimer,
				null,
				Timeout.Infinite,
				Timeout.Infinite);
		}

		/// <summary>
		/// Next run time, or <c>null</c> if there are no more runs.
		/// </summary>
		public DateTime? NextRun
		{
			get
			{
				lock (_sync)
				{
					return _nextRun;
				}
			}
		}

		/// <summary>
		/// Arms the timer for the first run.
		/// </summary>
		public void Start()
		{
			ScheduleNext(DateTime.Now);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			lock (_sync)
			{
				_isDisposed = true;
				_nextRun = null;
			}

			_timer.Dispose();
		}

		private void ScheduleNext(DateTime notBefore)
		{
			lock (_sync)
			{
				if (_isDisposed) return;

				_nextRun = _schedule.GetNextRun(notBefore);
				if (!_nextRun.HasValue)
				{
					_logger?.Info($"Command {_name} has no more runs");
					return;
				}

				_logger?.Info(
					$"Command {_name} next run: {_nextRun:yyyy-MM-dd HH:mm:ss}");
				Arm();
			}
		}

		private void Arm()
		{
			var delay = _nextRun.Value - DateTime.Now;
			if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
			if (delay > MaxStep) delay = MaxStep;

			_timer.Change(delay, Timeout.InfiniteTimeSpan);
		}

		private void HandleTimer(object state)
		{
			DateTime dueTime;
			lock (_sync)
			{
				if (_isDisposed || !_nextRun.HasValue) return;

				// An intermediate step of a long delay.
				if (DateTime.Now < _nextRun.Value)
				{
					Arm();
					return;
				}

				dueTime = _nextRun.Value;
			}

			// The next run is planned before this one is executed:
			// a long run must not shift the schedule. Runs missed
			// while the process was asleep are merged into one.
			var now = DateTime.Now;
			var last = now > dueTime ? now : dueTime;
			ScheduleNext(last.AddTicks(1));

			_onDue();
		}
	}
}
