using Bsa.Msa.Common;
using Bsa.Msa.Common.Helpers;
using Bsa.Msa.Common.Services.MessageHandling;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.RabbitMq.Core.Common
{
	internal sealed class InternalBus
	{
		private readonly ISerializeService _serializeService;
		private readonly ILocalLogger _localLogger;
		private readonly string _folder;
		private bool _isReady;
		private readonly object _lock = new object();
		private readonly ConcurrentDictionary<string, InternalBusItem> _concurrent =
			new ConcurrentDictionary<string, InternalBusItem>();

		// One journal per subscription folder; Lazy guarantees a single recovery of the folder.
		private readonly ConcurrentDictionary<string, Lazy<InternalBusJournal>> _journals =
			new ConcurrentDictionary<string, Lazy<InternalBusJournal>>();

		private readonly ConcurrentDictionary<string, string> _queueFolders =
			new ConcurrentDictionary<string, string>();

		private Timer _flushTimer;

		// Not _lock: Load holds _lock while it waits for a journal that may be opening on another thread.
		private readonly object _timerLock = new object();

		private const int JournalFlushIntervalMilliseconds = 10;
		private InternalBus(ISerializeService serializeService, ILocalLogger localLogger)
		{
			_serializeService = serializeService;
			_localLogger = localLogger;
			_folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InternalQueue");
			if (!Directory.Exists(_folder))
				Directory.CreateDirectory(_folder);
			// Acknowledgements are flushed in batches, the last ones are flushed on exit.
			AppDomain.CurrentDomain.ProcessExit += (sender, args) => FlushJournals(null);
			Task.Factory.StartNew(Load);
		}

		private static InternalBus _instance;
		private static readonly object _sync = new object();
		public static InternalBus Create(ISerializeService serializeService, ILocalLogger localLogger)
		{
			if (_instance == null)
			{
				lock (_sync)
				{
					if (_instance == null)
					{
						Interlocked.Exchange(ref _instance, new InternalBus(serializeService, localLogger));
					}
				}
			}

			return _instance;
		}

		public List<InternalBusItem> Get(string queue)
		{
			if (!_isReady)
				Load();
			return _concurrent.Values.Where(x => x.Queue == queue).ToList();
		}

		public void Load()
		{
			if (_isReady)
				return;
			lock (_lock)
			{
				if (_isReady)
					return;
				var files = Directory.GetFiles(_folder, "*.q", SearchOption.AllDirectories);
				Parallel.ForEach(files, file =>
				{
					try
					{

						var str = File.ReadAllText(file);

						var bytes = Convert.FromBase64String(str);
						var value = Encoding.UTF8.GetString(bytes);
						var obj = _serializeService.Deserialize<InternalBusItem>(value);
						if (obj.Id == null)
							obj.Id = Guid.NewGuid().ToString();
						obj.Headers = NormalizeHeaders(obj.Headers);
						obj.FileName = file;
						//if (_concurrent.ContainsKey(obj.Id))
						//{
						//	var temp = _concurrent[obj.ConsumerTag];
						//}

						_concurrent.TryAdd(obj.Id, obj);
					}
					catch (Exception e)
					{
						File.Delete(file);
						_localLogger?.Error($"{file};{e.Message}", e);
					}

				});
				LoadJournals();
				var groups = _concurrent.GroupBy(x => x.Value.Queue);
				foreach (var group in groups)
				{
					_localLogger?.Info($"Load from internal Queue name:{group.Key};Count:{group.Count()}");
				}
				_isReady = true;
			}
		}
		/// <summary>
		/// After a restart header values come back as JsonElement (or other
		/// serializer types) which the RabbitMQ client can not send: a retry
		/// of such a message failed and the message was lost.
		/// </summary>
		internal static IDictionary<string, object> NormalizeHeaders(IDictionary<string, object> headers)
		{
			var result = new Dictionary<string, object>();
			if (headers == null) return result;

			foreach (var pair in headers)
				result[pair.Key] = ToHeaderValue(pair.Value);
			return result;
		}

		private static object ToHeaderValue(object value)
		{
			switch (value)
			{
				case null:
				case string _:
				case byte[] _:
				case bool _:
				case int _:
				case long _:
				case double _:
				case decimal _:
					return value;
				case JsonElement element:
					return FromJson(element);
				default:
					return value.ToString();
			}
		}

		private static object FromJson(JsonElement element)
		{
			switch (element.ValueKind)
			{
				case JsonValueKind.Number:
					if (element.TryGetInt32(out var intValue)) return intValue;
					if (element.TryGetInt64(out var longValue)) return longValue;
					return element.GetDouble();
				case JsonValueKind.String:
					return element.GetString();
				case JsonValueKind.True:
					return true;
				case JsonValueKind.False:
					return false;
				case JsonValueKind.Null:
				case JsonValueKind.Undefined:
					return null;
				default:
					return element.GetRawText();
			}
		}

		public void Clear(string queue)
		{
			lock (_lock)
			{
				var forClear = _concurrent.Where(x => x.Value.Queue == queue).ToList();
				foreach (var keyValuePair in forClear)
				{
					// Ack removes the item and its file, otherwise it is loaded again after a restart.
					try
					{
						Ack(keyValuePair.Key);
					}
					catch (Exception e)
					{
						_localLogger?.Error($"Can not clear message {keyValuePair.Key}: {e.Message}", e);
					}
				}
			}
		}
		public int Count()
		{
			return _concurrent.Count;
		}

		private readonly ConcurrentDictionary<string, DateTime> _folders = new ConcurrentDictionary<string, DateTime>();
		public InternalBusItem Register(BasicDeliverEventArgs eventArgs, string queue, string messageAsString)
		{
			return Register(eventArgs, queue, messageAsString, InternalQueueFormat.Files);
		}

		public InternalBusItem Register(
			BasicDeliverEventArgs eventArgs,
			string queue,
			string messageAsString,
			InternalQueueFormat format)
		{
			var properties = eventArgs.BasicProperties;
			var headers = properties.Headers ?? new Dictionary<string, object>();
			var deliveryTag = eventArgs.DeliveryTag;
			var subFolder = GetQueueFolder(queue);
			var temp = DateTime.UtcNow;
			if (_folders.TryGetValue(subFolder, out temp))
			{
				if (temp <= DateTime.UtcNow)
					_folders.TryRemove(subFolder, out temp);
			}
			else if (!Directory.Exists(subFolder))
			{
				Directory.CreateDirectory(subFolder);
				_folders[subFolder] = DateTime.UtcNow.AddMinutes(10);
			}

			var id = Guid.NewGuid().ToString();
			var fileName = Path.Combine(subFolder, $"{id}.q");
			var obj = new InternalBusItem()
			{
				Id = id,
				DeliveryTag = deliveryTag,
				RoutingKey = eventArgs.RoutingKey,
				ConsumerTag = eventArgs.ConsumerTag,
				Exchange = eventArgs.Exchange,
				Headers = headers,
				FileName = fileName,
				Queue = queue,
				Body = messageAsString
			};
			if (format == InternalQueueFormat.Journal)
			{
				obj.FileName = null;
				AppendToJournal(subFolder, obj);
				return obj;
			}

			var str = _serializeService.Serialize(obj);

			File.WriteAllText(fileName, Convert.ToBase64String(Encoding.UTF8.GetBytes(str)));
			_concurrent.TryAdd(obj.Id, obj);
			return obj;
		}

		private void AppendToJournal(string folder, InternalBusItem item)
		{
			// Added first: compaction of the journal resolves live messages from the dictionary.
			_concurrent.TryAdd(item.Id, item);
			var journal = GetJournal(folder);
			try
			{
				lock (journal)
				{
					journal.Append(item);
				}
			}
			catch
			{
				_concurrent.TryRemove(item.Id, out _);
				throw;
			}
		}

		private string GetQueueFolder(string queue)
		{
			return _queueFolders.GetOrAdd(queue, x => Path.Combine(_folder, x.ComputeMd5String()));
		}

		private InternalBusJournal GetJournal(string folder)
		{
			return _journals.GetOrAdd(folder, x => new Lazy<InternalBusJournal>(() => OpenJournal(x))).Value;
		}

		private InternalBusJournal OpenJournal(string folder)
		{
			var journal = new InternalBusJournal(
				folder,
				_serializeService,
				_localLogger,
				id => _concurrent.TryGetValue(id, out var item) ? item : null);
			lock (journal)
			{
				foreach (var item in journal.Recover())
				{
					item.FileName = null;
					item.Headers = NormalizeHeaders(item.Headers);
					_concurrent.TryAdd(item.Id, item);
				}
			}
			lock (_timerLock)
			{
				_flushTimer ??= new Timer(
					FlushJournals,
					null,
					JournalFlushIntervalMilliseconds,
					JournalFlushIntervalMilliseconds);
			}
			return journal;
		}

		private void FlushJournals(object state)
		{
			foreach (var lazy in _journals.Values)
			{
				if (!lazy.IsValueCreated) continue;
				var journal = lazy.Value;
				try
				{
					lock (journal)
					{
						journal.Flush();
					}
				}
				catch (Exception e)
				{
					_localLogger?.Error($"Can not flush the internal queue journal: {e.Message}", e);
				}
			}
		}

		private void LoadJournals()
		{
			foreach (var folder in Directory.GetDirectories(_folder))
			{
				try
				{
					if (Directory.EnumerateFiles(folder, "*.qj").Any()) GetJournal(folder);
				}
				catch (Exception e)
				{
					_localLogger?.Error($"{folder};{e.Message}", e);
				}
			}
		}

		public void Ack(string id)
		{
			InternalBusItem val = null;
			if (_concurrent.TryRemove(id, out val))
			{
				if (val == null) return;
				// A message of the old format has its own file, a journal message has none.
				if (val.FileName != null)
				{
					if (File.Exists(val.FileName)) File.Delete(val.FileName);
					return;
				}
				var journal = GetJournal(GetQueueFolder(val.Queue));
				lock (journal)
				{
					journal.Ack(id);
				}
			}
		}
	}

	internal sealed class InternalBusItem
	{
		public string Id { get; set; }
		public string Queue { get; set; }
		public ulong DeliveryTag { get; set; }
		public string RoutingKey { get; set; }
		public string ConsumerTag { get; set; }
		public string Exchange { get; set; }
		public string Body { get; set; }
		public string FileName { get; set; }
		public IDictionary<string, object> Headers { get; set; }

	}
}
