using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Bsa.Msa.Common
{

	public class TimeValueContainer<TValue>
	{
		public TimeValueContainer(TValue value, int seconds)
		{
			Value = value;
			LastSendTime = DateTime.UtcNow.AddSeconds(seconds);
		}

		public DateTime LastSendTime { get; set; }

		public TValue Value { get; set; }
	}

	public interface IKeyBulkMessageContainer<TValue>
	{
		void AppendAdd(List<TValue> values, string key, int seconds);

		List<TValue> Get(string key);

		List<Tuple<string, List<TValue>>> GetAll();
	}

	public class KeyBulkMessageContainer<TValue> : IKeyBulkMessageContainer<TValue>
	{
		private readonly object _sync = new object();
		private ConcurrentDictionary<string, List<TimeValueContainer<TValue>>> _messages =
			new ConcurrentDictionary<string, List<TimeValueContainer<TValue>>>();
		
		public void AppendAdd(List<TValue> values, string key, int seconds)
		{
			lock (_sync)
			{
				var list = values.Select(x => new TimeValueContainer<TValue>(x, seconds)).ToList();

				if (!_messages.ContainsKey(key))
				{
					_messages.TryAdd(key, list);
				}
				else
				{
					_messages[key].AddRange(list);
				}
			}
		}

		public List<TValue> Get(string key)
		{
			List<TValue> result = new List<TValue>();
			lock (_sync)
			{
				if (_messages.ContainsKey(key))
				{
					var waitItems = _messages[key].Where(x => x.LastSendTime > DateTime.UtcNow).ToList();
					result.AddRange(_messages[key].Where(x=> !waitItems.Contains(x)).Select(x=>x.Value));
					Console.WriteLine($"Total items {_messages[key].Count}. Wait  items {waitItems.Count}. Order items {result.Count}.");
					_messages[key] = waitItems;
				}
			}
			return result;
		}

		public List<Tuple<string, List<TValue>>> GetAll()
		{
			var result = new List<Tuple<string, List<TValue>>>();
			lock (_sync)
			{
				foreach (var message in _messages)
				{
					result.Add(new Tuple<string, List<TValue>>(message.Key,message.Value.Select(x=>x.Value).ToList()));
				}
				_messages = new ConcurrentDictionary<string, List<TimeValueContainer<TValue>>>();
			}
			return result;
		}
	}
}