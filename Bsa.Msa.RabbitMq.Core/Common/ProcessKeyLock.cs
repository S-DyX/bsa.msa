using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.Common
{

	public sealed class ProcessKeyLock
	{
		private ProcessKeyLock()
		{
		}
		private static readonly object _sync = new object();

		private static ProcessKeyLock _instance;
		public static ProcessKeyLock Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (_sync)
					{
						if (_instance == null)
						{
							_instance = new ProcessKeyLock();
						}
					}
				}
				return _instance;
			}
		}
		public bool IsLock(string key)
		{
			return _process.ContainsKey(key);
		}

		public void WaitWhileIsLock(string key, int waitInterval = 500)
		{
			while (_process.ContainsKey(key))
			{
				Console.WriteLine($"Sleep:{key}");
				Task.Delay(waitInterval);
			}
		}
		public void WaitWhileIsLockTimes(string key, int times, int waitInterval = 500)
		{
			int i = 0;
			while (_process.ContainsKey(key) && times > i)
			{
				Console.WriteLine($"Sleep:{key}");
				Task.Delay(waitInterval);
				i++;
			}
		}
		public void RegisterWait(string key, int waitInterval = 500)
		{
			object localSync = null;
			// блокируем основные потоки
			lock (_syncInstance)
			{
				// пытаемся получить локальную блокировку
				if (_process.ContainsKey(key))
					localSync = _process[key];
				else
				{
					// если блокировки нет создаем и выходим
					_process[key] = new object();
					return;
				}
			}
			// если блокировка есть
			lock (localSync)
			{
				// ждем пока ключ не удалят
				while (_process.ContainsKey(key))
				{
					Console.WriteLine($"Sleep:{key}");
					Thread.Sleep(waitInterval);
				}
				// после удаления ключа, с блокировкой всех потоков добавляем
				lock (_syncInstance)
				{
					_process[key] = new object();
				}
			}
			//if (!_process.ContainsKey(key))
			//{
			//	lock (_syncInstance)
			//	{
			//		if (!_process.ContainsKey(key))
			//		{
			//			_process.Add(key, new object());
			//			Console.WriteLine($"Register:{key}");
			//		}
			//	}
			//}
		}

		public void Register(string key)
		{
			Check(key);
			lock (_syncInstance)
			{
				Check(key);
				_process.Add(key, new object());
			}
		}
		public void Release(string key)
		{
			if (_process.ContainsKey(key))
			{
				lock (_syncInstance)
				{
					if (_process.ContainsKey(key))
					{
						Console.WriteLine($"Release:{key}");
						_process.Remove(key);
					}
				}
			}
		}

		private void Check(string key)
		{
			if (_process.ContainsKey(key))
			{
				throw new InvalidOperationException("Key is already used:" + key);
			}
		}


		private readonly object _syncInstance = new object();

		private readonly Dictionary<string, object> _process = new Dictionary<string, object>();
	}
}
