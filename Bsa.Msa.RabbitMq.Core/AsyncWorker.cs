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
		private Thread _thread;
		private bool _isRun;
		private bool _isActive;
		private DateTime _lastAction;
		public DateTime LastAction => _lastAction;
		public AsyncWorker(ILocalLogger localLogger, ConcurrentQueue<Action> queue)
		{
			_lastAction = DateTime.UtcNow;
			_localLogger = localLogger;
			_queue = queue;
			_isRun = true;
			_thread = new Thread(Process);
			_thread.Start();
		}
		public bool IsActive => _isActive;
		private int _sleepTimes = 0;

		public bool CanFree => !_isActive && _lastAction < DateTime.UtcNow.AddSeconds(-5);
		private void Process()
		{
			while (_isRun)
			{
				try
				{
					if (_queue.TryDequeue(out var action))
					{
						_sleepTimes = 0;
						_isActive = true;
						if (action != null)
						{
							_lastAction = DateTime.UtcNow;
							action.Invoke();
							_lastAction = DateTime.UtcNow;
						}
					}
					else
					{
						if (_sleepTimes < 10)
							_sleepTimes++;
						_isActive = false;
						Thread.Sleep(100 * _sleepTimes);
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
