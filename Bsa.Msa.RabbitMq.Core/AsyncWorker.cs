using Bsa.Msa.Common;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Bsa.Msa.RabbitMq.Core
{
	public sealed class AsyncWorker : IDisposable
	{
		private readonly ILocalLogger _localLogger;
		private readonly ConcurrentQueue<Action> _queue;
		private readonly Action<AsyncWorker> _release;
		private readonly string _name;
		private Thread _thread;
		private bool _isRun;
		private bool _isActive;
		private DateTime _lastAction;
		private readonly SemaphoreSlim _signal;
		private const int MaxIdleIterations = 50;
		private const int IdleWaitMilliseconds = 1000;

		//public delegate void Release(AsyncWorker worker);

		//public event AsyncWorker.Release _releaseEvent;
		public DateTime LastAction => _lastAction;

		public AsyncWorker(ILocalLogger localLogger, ConcurrentQueue<Action> queue, Action<AsyncWorker> release, string name)
			: this(localLogger, queue, release, name, null)
		{
		}

		/// <summary>
		/// Creates a worker that sleeps until <paramref name="signal"/> is released
		/// instead of polling the queue.
		/// </summary>
		/// <param name="localLogger">Logger.</param>
		/// <param name="queue">Shared work queue.</param>
		/// <param name="release">Called when the worker leaves the pool after idling.</param>
		/// <param name="name">Queue name for logging.</param>
		/// <param name="signal">Released once per queued action; <c>null</c> means polling.</param>
		internal AsyncWorker(
			ILocalLogger localLogger,
			ConcurrentQueue<Action> queue,
			Action<AsyncWorker> release,
			string name,
			SemaphoreSlim signal)
		{
			_signal = signal;
			_lastAction = DateTime.UtcNow;
			_localLogger = localLogger;
			_queue = queue;
			_release = release;
			_name = name;
			_isRun = true;
			_thread = new Thread(Process);
			_thread.Start();
		}
		public bool IsActive => _isActive;
		private int _sleepTimes = 0;

		public bool CanFree => !_isActive && _lastAction < DateTime.UtcNow.AddSeconds(-30);

		public void Stop()
		{
			_localLogger?.Debug($"Stop process {_name} Worker ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
			_isRun = false;
			_isActive = false;

		}

		private void Process()
		{
			while (_isRun)
			{
				try
				{
					if (_queue.TryDequeue(out var action))
					{
						if (action != null)
						{
							_localLogger?.Debug($"Start process {_name} Worker ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
							_isActive = true;
							_sleepTimes = 0;
							_lastAction = DateTime.UtcNow;
							action.Invoke();
							_lastAction = DateTime.UtcNow;
							_isActive = false;
							_localLogger?.Debug($"End process {_name} Worker ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
						}

					}
					else if (!WaitForWork())
					{
						// No work for the whole idle period: the worker leaves the pool.
						_isRun = false;
						_isActive = false;
						_release?.Invoke(this);
						return;
					}
				}
				catch (Exception ex)
				{
					_localLogger?.Error(ex.Message, ex);
				}
				finally
				{
					_isActive = false;
				}
			}
		}

		/// <summary>
		/// Waits for the next queued action.
		/// Returns <c>false</c> when the worker has been idle long enough to be released.
		/// </summary>
		private bool WaitForWork()
		{
			var lastAction = (DateTime.UtcNow - _lastAction).Seconds;
			_localLogger?.Debug($"Worker {_name} sleep ManagedThreadId:{Thread.CurrentThread.ManagedThreadId};since the last method call {lastAction} sec");
			_isActive = false;
			if (_sleepTimes >= MaxIdleIterations) return false;
			var sleepTimes = _sleepTimes++;
			if (_signal != null)
			{
				// Polling with growing sleeps delayed every message by up to a second;
				// the signal wakes the worker as soon as an action is queued.
				if (_signal.Wait(IdleWaitMilliseconds)) _sleepTimes = 0;
				return true;
			}
			if (sleepTimes > 10) sleepTimes = 10;
			Thread.Sleep(100 * sleepTimes);
			return true;
		}

		public void Dispose()
		{
			_isRun = false;
			_thread = null;
			//_thread.Abort();
		}
	}
}
