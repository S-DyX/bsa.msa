using Bsa.Msa.Common.Services.Commands;
using System;
using System.Threading;

namespace Bsa.Msa.Common.Repeaters
{
	public sealed class DisallowConcurrentModeRepeater : IRepeater
	{
		private readonly ILocalLogger _logger;

		private class OperationsQueue
		{
			private int _operationsQueued;

			private readonly int _maxQueueLength;

			private readonly object _syncLock = new object();

			private readonly Action<CancellationToken> _action;

			public OperationsQueue(int maxQueueLength, Action<CancellationToken> onRepeat)
			{
				this._maxQueueLength = maxQueueLength;
				this._action = onRepeat;
			}

		
			public void Enqueue()
			{
				lock (_syncLock)
				{
					if (_operationsQueued < this._maxQueueLength)
						_operationsQueued++;
				}
			}

			
			public Action<CancellationToken> Dequeue()
			{
				lock (_syncLock)
				{
					if (0 == _operationsQueued)
						return null;

					_operationsQueued--;
					return _action;
				}
			}

			public bool HasPending
			{
				get
				{
					lock (_syncLock)
					{
						return _operationsQueued > 0;
					}
				}
			}
		}

		private const int MaxQueueLength = 1;

		private readonly ICommandSettings _commandSettings;

		private OperationsQueue _operationsQueue;

		// 1 while a thread drains the queue; changed only through Interlocked.
		private int _isProcessing;

		private ScheduleTimer _timer;

		private string _name;

		private readonly CancellationTokenSource _cancellationTokenSource;

		public DisallowConcurrentModeRepeater(ICommandSettings commandSettings, ILocalLogger logger = null)
		{
			_logger = logger;
			_commandSettings = commandSettings;
			_name = commandSettings.Name;
			this._cancellationTokenSource = new CancellationTokenSource();
		}


		public void Dispose()
		{
			_cancellationTokenSource.Cancel();

			if (null != this._timer)
				this._timer.Dispose();
		}

		public void Start(Action<CancellationToken> onRepeat)
		{
			if (onRepeat == null)
				throw new ArgumentNullException($"onRepeat {_name}");

			this._operationsQueue = new OperationsQueue(MaxQueueLength, onRepeat);

			this.CreateTimerOnce();
		}

		public event UnhandledExceptionEventHandler Error;

		private void CreateTimerOnce()
		{
			if (null == this._timer)
			{
				this._timer = new ScheduleTimer(
					_commandSettings.Schedule,
					HandleTimerCallback,
					_logger,
					_name);
				this._timer.Start();
			}
		}

		private void HandleTimerCallback()
		{
			try
			{
				this._operationsQueue.Enqueue();

				// Re-check after release: an operation enqueued while the previous
				// owner was finishing would otherwise be lost.
				while (this._operationsQueue.HasPending
					&& Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
				{
					try
					{
						DrainQueue();
					}
					finally
					{
						Volatile.Write(ref _isProcessing, 0);
					}
				}
			}
			catch (Exception ex)
			{
				_logger?.Error($"Name:{_name};Message:{ex.Message}", ex);
			}

		}

		private void DrainQueue()
		{
			var token = _cancellationTokenSource.Token;
			var operation = this._operationsQueue.Dequeue();
			while (null != operation && !token.IsCancellationRequested)
			{
				try
				{
					operation(token);
				}
				catch (Exception e)
				{
					_logger?.Error($"Name:{_name};Message:{e.Message}", e);
					RaiseError(e);
				}

				operation = this._operationsQueue.Dequeue();
			}
		}

		private void RaiseError(Exception ex)
		{
			var handler = this.Error;
			if (null != handler)
				handler(this, new UnhandledExceptionEventArgs(ex, false));
		}
	}
}
