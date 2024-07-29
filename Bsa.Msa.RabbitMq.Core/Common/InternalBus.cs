using Bsa.Msa.Common;
using Bsa.Msa.Common.Helpers;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
		private ConcurrentDictionary<ulong, InternalBusItem> _concurrent =
			new ConcurrentDictionary<ulong, InternalBusItem>();
		public InternalBus(ISerializeService serializeService, ILocalLogger localLogger)
		{
			_serializeService = serializeService;
			_localLogger = localLogger;
			_folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "InternalQueue");
			if (!Directory.Exists(_folder))
				Directory.CreateDirectory(_folder);
			Task.Factory.StartNew(Load);
		}

		public List<InternalBusItem> Get(string queue)
		{
			if (!_isReady)
				Load();
			return _concurrent.Values.Where(x => x.Queue == queue).ToList();
		}

		public void Load()
		{
			lock (_lock)
			{
				var files = Directory.GetFiles(_folder, "*.q", SearchOption.AllDirectories);
				Parallel.ForEach(files, file =>
				{
					try
					{

						var str = File.ReadAllText(file);

						var bytes = Convert.FromBase64String(str);
						var value = Encoding.UTF8.GetString(bytes);
						var obj = _serializeService.Deserialize<InternalBusItem>(value);
						obj.FileName = file;
						_concurrent.TryAdd(obj.DeliveryTag, obj);
					}
					catch (Exception e)
					{
						File.Delete(file);
						_localLogger?.Error($"{file};{e.Message}", e);
					}

				});
				_isReady = true;
			}
		}

		private readonly ConcurrentDictionary<string, DateTime> _folders = new ConcurrentDictionary<string, DateTime>();
		public InternalBusItem Register(BasicDeliverEventArgs eventArgs, string queue, string messageAsString)
		{
			var properties = eventArgs.BasicProperties;
			var headers = properties.Headers ?? new Dictionary<string, object>();
			var id = eventArgs.DeliveryTag;
			var subFolder = Path.Combine(_folder, queue.ComputeMd5String());
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

			var fileName = Path.Combine(subFolder, $"{Guid.NewGuid()}.q");
			var obj = new InternalBusItem()
			{
				DeliveryTag = id,
				RoutingKey = eventArgs.RoutingKey,
				ConsumerTag = eventArgs.ConsumerTag,
				Exchange = eventArgs.Exchange,
				Headers = headers,
				FileName = fileName,
				Queue = queue,
				Body = messageAsString
			};
			var str = _serializeService.Serialize(obj);

			File.WriteAllText(fileName, Convert.ToBase64String(Encoding.UTF8.GetBytes(str)));
			_concurrent.TryAdd(id, obj);
			return obj;
		}

		public void Ack(ulong id)
		{
			InternalBusItem val = null;
			if (_concurrent.TryRemove(id, out val))
			{
				if (val != null && File.Exists(val.FileName))
				{
					File.Delete(val.FileName);
				}

			}
		}
	}

	internal sealed class InternalBusItem
	{
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
