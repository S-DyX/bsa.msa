using Bsa.Msa.Common.Services.Commands;
using System;
using System.Threading;

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
	/// Represents the repeater.
	/// </summary>
	public sealed class AllowConcurrenceModeRepeater : IRepeater
	{
		private readonly TimeSpan _dueTime;

		private readonly TimeSpan _period;

		private readonly CancellationTokenSource _cancellationTokenSource;

		private Timer _timer;

		private bool _isStarted = false;

		private Action<CancellationToken> _repeatAction;
		public AllowConcurrenceModeRepeater(ICommandSettings commandSettings)
		{
			this._dueTime = commandSettings.DueTime;
			this._period = commandSettings.Period;

			this._cancellationTokenSource = new CancellationTokenSource();
		}

		public void Start(Action<CancellationToken> onRepeat)
		{
			if (onRepeat == null)
				throw new ArgumentNullException("onRepeat");

			if (_isStarted)
				throw new InvalidOperationException("The repeater is already started.");

			this._repeatAction = onRepeat;
			var diff = DateTime.Now.Date - _dueTime - DateTime.Now;
			if (diff.TotalMilliseconds <= 0)
				diff = new TimeSpan(0);

			_timer = new Timer(HandleTimerCallback, null, diff, _period);

			_isStarted = true;
		}

		/// <summary>
		/// The handler for the unhandled exceptions during repeating.
		/// </summary>
		public event UnhandledExceptionEventHandler Error;

		/// <summary>
		/// Disposes the _timer.
		/// </summary>
		public void Dispose()
		{
			_cancellationTokenSource.Cancel();

			if (_timer != null)
				_timer.Dispose();
		}

		private void HandleTimerCallback(object state)
		{
			try
			{
				_repeatAction(_cancellationTokenSource.Token);
			}
			catch (Exception e)
			{
				if (Error != null)
					Error(this, new UnhandledExceptionEventArgs(e, false));
			}
		}
	}
}
