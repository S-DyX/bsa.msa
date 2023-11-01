using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bsa.Msa.Common.Interfaces;

namespace Bsa.Msa.Common
{
	

	public class BulkMessageContainer<TValue> : IBulkMessageContainer<TValue>
	{
		private const int Size = 100;
		private DateTime _lastSendTime;
		private readonly object _sync = new object();
		private ConcurrentDictionary<string, TValue> _messages;

		public BulkMessageContainer()
		{
			_messages = new ConcurrentDictionary<string, TValue>();
		}
		public void Add(List<TValue> messages, Func<TValue, string> getKey)
		{
			lock (_sync)
			{
				foreach (var value in messages)
				{
					var key = getKey.Invoke(value);
					if (!_messages.ContainsKey(key))
						_messages.TryAdd(key, value);
				}
			}
		}

		public List<TValue> Get(int size)
		{
			return Get(size, 300);
		}

		public List<TValue> Get(int size, int seconds)
		{
			var result = new List<TValue>();
			if (_messages.Count > size || _lastSendTime < DateTime.UtcNow)
			{
				lock (_sync)
				{
					result = _messages.Values.ToList();
					_messages = new ConcurrentDictionary<string, TValue>();
					_lastSendTime = DateTime.UtcNow.AddSeconds(seconds);
				}
			}
			return result;
		}
		public List<TValue> Get()
		{
			return Get(Size);
		}

		public int Count()
		{
			return _messages.Count;
		}
	}
}
