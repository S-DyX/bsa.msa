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

		//public delegate void Release(AsyncWorker worker);

		//public event AsyncWorker.Release _releaseEvent;
		public DateTime LastAction => _lastAction;

		public AsyncWorker(ILocalLogger localLogger, ConcurrentQueue<Action> queue, Action<AsyncWorker> release, string name)
		{
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
					else
					{
						var lastAction = (DateTime.UtcNow-_lastAction).Seconds;
						_localLogger?.Debug($"Worker {_name} sleep ManagedThreadId:{Thread.CurrentThread.ManagedThreadId};since the last method call {lastAction} sec");
						var sleepTimes = _sleepTimes;
						if (_sleepTimes < 50)
						{
							_sleepTimes++;
						}
						else
						{
							_isRun = false;
							_isActive = false;
							_release?.Invoke(this);
							return;
						}

						if (sleepTimes > 10)
							sleepTimes = 10;
						_isActive = false;
						Thread.Sleep(100 * sleepTimes);
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

		public void Dispose()
		{
			_isRun = false;
			_thread = null;
			//_thread.Abort();
		}
	}
}
