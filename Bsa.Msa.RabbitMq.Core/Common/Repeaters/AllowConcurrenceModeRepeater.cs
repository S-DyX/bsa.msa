using System;
using System.Threading;
using Bsa.Msa.Common.Services.Commands;

namespace Bsa.Msa.Common.Repeaters
{
	/// <summary>
	/// Represents behaviors for execution operations with concurrency.
	/// </summary>
	public enum RepeaterConcurrentMode
	{
		AllowConcurrenceMode,

		DisallowConcurrentMode
	}


	public delegate void OnRepeat(CancellationToken cancellationToken);

	/// <summary>
	/// Runs the action by the command schedule; a new run may start
	/// while the previous one is still running.
	/// </summary>
	public sealed class AllowConcurrenceModeRepeater : IRepeater
	{
		private readonly ICommandSettings _commandSettings;

		private readonly ILocalLogger _logger;

		private readonly CancellationTokenSource _cancellationTokenSource;

		private readonly string _name;

		private ScheduleTimer _timer;

		private bool _isStarted;

		private Action<CancellationToken> _repeatAction;

		/// <summary>
		/// Creates the repeater.
		/// </summary>
		/// <param name="commandSettings">Command settings with the schedule.</param>
		/// <param name="logger">Logger.</param>
		public AllowConcurrenceModeRepeater(
			ICommandSettings commandSettings,
			ILocalLogger logger = null)
		{
			_commandSettings = commandSettings;
			_logger = logger;
			_name = commandSettings.Name;
			_cancellationTokenSource = new CancellationTokenSource();
		}

		/// <inheritdoc/>
		public void Start(Action<CancellationToken> onRepeat)
		{
			if (onRepeat == null)
				throw new ArgumentNullException(nameof(onRepeat));

			if (_isStarted)
				throw new InvalidOperationException("The repeater is already started.");

			_repeatAction = onRepeat;
			_timer = new ScheduleTimer(
				_commandSettings.Schedule,
				HandleTimerCallback,
				_logger,
				_name);
			_timer.Start();
			_isStarted = true;
		}

		/// <summary>
		/// The handler for the unhandled exceptions during repeating.
		/// </summary>
		public event UnhandledExceptionEventHandler Error;

		/// <summary>
		/// Stops the timer and cancels the running actions.
		/// </summary>
		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
			_timer?.Dispose();
		}

		private void HandleTimerCallback()
		{
			if (_cancellationTokenSource.IsCancellationRequested) return;

			try
			{
				_repeatAction(_cancellationTokenSource.Token);
			}
			catch (Exception e)
			{
				_logger?.Error($"Name:{_name};Message:{e.Message}", e);
				Error?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
			}
		}
	}
}
