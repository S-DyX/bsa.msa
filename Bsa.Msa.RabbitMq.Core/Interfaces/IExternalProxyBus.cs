using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	/// <summary>
	/// File-based <see cref="ISimpleBus"/> without a broker: queues and
	/// exchanges are folders, messages are JSON files. A message delivered
	/// to a subscriber or read by <see cref="GetMessageExchange{TMessage}(string)"/>
	/// is removed from the queue. Intended for tests and local runs.
	/// </summary>
	public sealed class ExternalProxyBus : ISimpleBus
	{
		private readonly string _basePath;

		private readonly ISimpleBusNaming _busNaming;

		private readonly ConcurrentDictionary<string, List<Action<object>>> _subscribers =
			new ConcurrentDictionary<string, List<Action<object>>>();

		// Guards read-modify-write of the bindings files.
		private readonly object _fileLock = new object();

		private bool _disposed;

		private const string ConfigFileName = "config.json";

		private const string BindingsFileName = "bindings.json";

		private const string AllTopics = "#";

		private static readonly JsonSerializerOptions WriteOptions =
			new JsonSerializerOptions { WriteIndented = true };

		/// <summary>
		/// Creates the bus.
		/// </summary>
		/// <param name="basePath">Root folder; "SimpleBus" in the current folder by default.</param>
		/// <param name="busNaming">Queue and exchange naming, the same as for RabbitMQ by default.</param>
		public ExternalProxyBus(string basePath = null, ISimpleBusNaming busNaming = null)
		{
			_basePath = basePath ?? Path.Combine(Directory.GetCurrentDirectory(), "SimpleBus");
			_busNaming = busNaming ?? new DefaultSimpleBusNaming();
			Directory.CreateDirectory(Path.Combine(_basePath, "Queues"));
			Directory.CreateDirectory(Path.Combine(_basePath, "Exchanges"));
		}

		/// <inheritdoc />
		public bool IsModel => true;

		/// <inheritdoc />
		public bool IsConnected => true;

		/// <inheritdoc />
		public Action Shutdown { get; set; }

		/// <inheritdoc />
		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;
			Shutdown?.Invoke();
		}

		/// <inheritdoc />
		public void Send<TMessage>(TMessage message, int? ttl = null) where TMessage : class
		{
			Send(_busNaming.GetQueueName<TMessage>(), message, ttl);
		}

		/// <inheritdoc />
		public void Send<TMessage>(string queue, TMessage message, int? ttl = null) where TMessage : class
		{
			EnsureQueue(queue);
			Deliver(queue, message, ttl);
		}

		/// <inheritdoc />
		public void SendSelf<TMessage>(TMessage message) where TMessage : class
		{
			Send(message, null);
		}

		/// <inheritdoc />
		public void Publish<TMessage>(TMessage message) where TMessage : class
		{
			Publish(message, string.Empty, null, null);
		}

		/// <inheritdoc />
		public void Publish<TMessage>(TMessage message, string topic, string exchangeName = null) where TMessage : class
		{
			Publish(message, topic, exchangeName, null);
		}

		/// <inheritdoc />
		public void Publish<TMessage>(TMessage message, string topic, string exchangeName, int? ttl) where TMessage : class
		{
			var exchange = string.IsNullOrEmpty(exchangeName)
				? _busNaming.GetExchangeName<TMessage>()
				: exchangeName;
			EnsureExchange(exchange);
			foreach (var binding in GetBindings(exchange))
			{
				if (!MatchesTopic(topic, binding.TopicPattern)) continue;

				EnsureQueue(binding.QueueName);
				Deliver(binding.QueueName, message, ttl);
			}
		}

		/// <inheritdoc />
		public void Subscribe<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
		{
			EnsureQueue(queueName);
			var list = _subscribers.GetOrAdd(queueName, _ => new List<Action<object>>());
			lock (list)
			{
				list.Add(msg => action((TMessage)msg));
			}
		}

		/// <inheritdoc />
		public void SubscribeExchange<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
		{
			Subscribe(queueName, action, messageHandlerSettings);

			var exchange = _busNaming.GetExchangeName<TMessage>();
			var pattern = string.IsNullOrEmpty(messageHandlerSettings?.RoutingKey)
				? AllTopics
				: messageHandlerSettings.RoutingKey;
			EnsureExchange(exchange);
			lock (_fileLock)
			{
				var bindings = GetBindings(exchange);
				if (bindings.Any(b => b.QueueName == queueName && b.TopicPattern == pattern))
					return;

				bindings.Add(new Binding { QueueName = queueName, TopicPattern = pattern });
				SaveBindings(exchange, bindings);
			}
		}

		/// <inheritdoc />
		public List<TMessage> GetMessageExchange<TMessage>(string queueName)
		{
			return TakeMessages<TMessage>(queueName, null);
		}

		/// <inheritdoc />
		public List<TMessage> GetMessageExchange<TMessage>(string queueName, int count)
		{
			return TakeMessages<TMessage>(queueName, count);
		}

		/// <inheritdoc />
		public void Delete<TMessage>(string queue) where TMessage : class
		{
			Delete(queue);
			DeleteExchange(_busNaming.GetExchangeName<TMessage>());
		}

		/// <inheritdoc />
		public void Delete<TMessage>() where TMessage : class
		{
			DeleteExchange(_busNaming.GetExchangeName<TMessage>());
		}

		/// <inheritdoc />
		public void Delete(string queue)
		{
			var queuePath = GetQueuePath(queue);
			if (Directory.Exists(queuePath))
				Directory.Delete(queuePath, true);
			_subscribers.TryRemove(queue, out _);

			var exchangesDir = Path.Combine(_basePath, "Exchanges");
			if (!Directory.Exists(exchangesDir)) return;

			lock (_fileLock)
			{
				foreach (var exchangeDir in Directory.GetDirectories(exchangesDir))
				{
					var bindingsPath = Path.Combine(exchangeDir, BindingsFileName);
					var bindings = ReadBindings(bindingsPath);
					var newBindings = bindings.Where(b => b.QueueName != queue).ToList();
					if (newBindings.Count != bindings.Count)
						File.WriteAllText(bindingsPath, JsonSerializer.Serialize(newBindings, WriteOptions));
				}
			}
		}

		/// <inheritdoc />
		public void Purge(string queueName)
		{
			var queuePath = GetQueuePath(queueName);
			if (!Directory.Exists(queuePath)) return;

			foreach (var file in GetMessageFiles(queuePath))
				TryDelete(file);
		}

		/// <inheritdoc />
		public void Reconnect(string name = null)
		{
			// Nothing to reconnect in the file implementation.
		}

		/// <inheritdoc />
		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
			where TRequest : class
			where TResponse : class
		{
			return Respond(response, null);
		}

		/// <inheritdoc />
		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class
		{
			var requestQueue = queueName ?? _busNaming.GetQueueName<TRequest>();
			var responseQueue = requestQueue + ".responses";
			Action<object> handler = obj => Send(responseQueue, response((TRequest)obj));

			EnsureQueue(requestQueue);
			var list = _subscribers.GetOrAdd(requestQueue, _ => new List<Action<object>>());
			lock (list)
			{
				list.Add(handler);
			}

			return new DisposableAction(() =>
			{
				lock (list)
				{
					list.Remove(handler);
				}
			});
		}

		private void Deliver<TMessage>(string queueName, TMessage message, int? ttl)
		{
			var file = SaveMessage(queueName, message, ttl);

			// A delivered message is consumed; otherwise it waits in the queue.
			if (InvokeSubscribers(queueName, message))
				TryDelete(file);
		}

		private bool InvokeSubscribers<TMessage>(string queueName, TMessage message)
		{
			if (!_subscribers.TryGetValue(queueName, out var list)) return false;

			Action<object>[] actions;
			lock (list)
			{
				actions = list.ToArray();
			}

			if (actions.Length == 0) return false;

			var isDelivered = true;
			foreach (var action in actions)
			{
				try
				{
					action(message);
				}
				catch
				{
					// A handler must not break the bus; the message stays in the queue.
					isDelivered = false;
				}
			}

			return isDelivered;
		}

		private string SaveMessage<TMessage>(string queueName, TMessage message, int? ttl)
		{
			var envelope = new MessageEnvelope<TMessage>
			{
				Body = message,
				CreatedAt = DateTime.UtcNow,
				TtlMilliseconds = ttl
			};
			var json = JsonSerializer.Serialize(envelope, WriteOptions);
			var filePath = Path.Combine(GetQueuePath(queueName), $"{Guid.NewGuid():N}.json");
			File.WriteAllText(filePath, json);
			return filePath;
		}

		private List<TMessage> TakeMessages<TMessage>(string queueName, int? count)
		{
			var queuePath = GetQueuePath(queueName);
			if (!Directory.Exists(queuePath)) return new List<TMessage>();

			var items = new List<KeyValuePair<string, MessageEnvelope<TMessage>>>();
			foreach (var file in GetMessageFiles(queuePath))
			{
				var envelope = ReadEnvelope<TMessage>(file);
				if (envelope == null) continue;

				if (envelope.ExpiresAt < DateTime.UtcNow)
				{
					TryDelete(file);
					continue;
				}

				items.Add(new KeyValuePair<string, MessageEnvelope<TMessage>>(file, envelope));
			}

			var ordered = items.OrderBy(x => x.Value.CreatedAt);
			var taken = (count > 0 ? ordered.Take(count.Value) : ordered).ToList();

			// Like BasicGet with ack: the returned messages leave the queue.
			foreach (var item in taken)
				TryDelete(item.Key);

			return taken.Select(x => x.Value.Body).ToList();
		}

		private static MessageEnvelope<TMessage> ReadEnvelope<TMessage>(string file)
		{
			try
			{
				var json = File.ReadAllText(file);
				return JsonSerializer.Deserialize<MessageEnvelope<TMessage>>(json);
			}
			catch
			{
				// A broken file is skipped.
				return null;
			}
		}

		private static IEnumerable<string> GetMessageFiles(string queuePath)
		{
			// config.json lives in the same folder and is not a message.
			return Directory.GetFiles(queuePath, "*.json")
				.Where(x => !string.Equals(Path.GetFileName(x), ConfigFileName, StringComparison.OrdinalIgnoreCase));
		}

		private string GetQueuePath(string queueName)
		{
			return Path.Combine(_basePath, "Queues", ToFileName(queueName));
		}

		private string GetExchangePath(string exchangeName)
		{
			return Path.Combine(_basePath, "Exchanges", ToFileName(exchangeName));
		}

		private string GetExchangeBindingsPath(string exchangeName)
		{
			return Path.Combine(GetExchangePath(exchangeName), BindingsFileName);
		}

		// Default names contain ':' which is not allowed in Windows file names.
		private static string ToFileName(string name)
		{
			var invalid = Path.GetInvalidFileNameChars();
			return new string(name.Select(x => invalid.Contains(x) ? '_' : x).ToArray());
		}

		private void EnsureQueue(string queueName)
		{
			var path = GetQueuePath(queueName);
			if (Directory.Exists(path)) return;

			Directory.CreateDirectory(path);
			var json = JsonSerializer.Serialize(new { Durable = true });
			File.WriteAllText(Path.Combine(path, ConfigFileName), json);
		}

		private void EnsureExchange(string exchangeName)
		{
			var path = GetExchangePath(exchangeName);
			if (Directory.Exists(path)) return;

			Directory.CreateDirectory(path);
			var json = JsonSerializer.Serialize(new { Type = "topic", Durable = true });
			File.WriteAllText(Path.Combine(path, ConfigFileName), json);
			File.WriteAllText(Path.Combine(path, BindingsFileName), "[]");
		}

		private void DeleteExchange(string exchangeName)
		{
			var path = GetExchangePath(exchangeName);
			lock (_fileLock)
			{
				if (Directory.Exists(path))
					Directory.Delete(path, true);
			}
		}

		private List<Binding> GetBindings(string exchangeName)
		{
			return ReadBindings(GetExchangeBindingsPath(exchangeName));
		}

		private static List<Binding> ReadBindings(string path)
		{
			if (!File.Exists(path)) return new List<Binding>();

			try
			{
				var json = File.ReadAllText(path);
				return JsonSerializer.Deserialize<List<Binding>>(json) ?? new List<Binding>();
			}
			catch
			{
				return new List<Binding>();
			}
		}

		private void SaveBindings(string exchangeName, List<Binding> bindings)
		{
			var json = JsonSerializer.Serialize(bindings, WriteOptions);
			File.WriteAllText(GetExchangeBindingsPath(exchangeName), json);
		}

		// An empty topic is a fanout publish: every binding receives it.
		private static bool MatchesTopic(string topic, string pattern)
		{
			if (string.IsNullOrEmpty(topic)) return true;
			if (pattern == AllTopics || pattern == "*") return true;
			return topic == pattern;
		}

		private static void TryDelete(string file)
		{
			try
			{
				File.Delete(file);
			}
			catch (IOException)
			{
				// The file is in use or already deleted.
			}
		}

		private sealed class MessageEnvelope<T>
		{
			public T Body { get; set; }

			public DateTime CreatedAt { get; set; }

			// The same unit as RabbitMQ TTL: milliseconds.
			public int? TtlMilliseconds { get; set; }

			public DateTime? ExpiresAt => TtlMilliseconds.HasValue
				? CreatedAt.AddMilliseconds(TtlMilliseconds.Value)
				: (DateTime?)null;
		}

		private sealed class Binding
		{
			public string QueueName { get; set; }

			public string TopicPattern { get; set; }
		}

		private sealed class DisposableAction : IDisposable
		{
			private Action _disposeAction;

			public DisposableAction(Action action)
			{
				_disposeAction = action;
			}

			public void Dispose()
			{
				_disposeAction?.Invoke();
				_disposeAction = null;
			}
		}
	}
}
