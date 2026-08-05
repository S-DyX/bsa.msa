using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Interfaces;

public sealed class ExternalProxyBus : ISimpleBus
{
	private readonly string _basePath;
	private readonly ConcurrentDictionary<string, List<Action<object>>> _subscribers = new();
	private readonly object _fileLock = new object();
	private bool _disposed;

	public ExternalProxyBus(string basePath = null)
	{
		_basePath = basePath ?? Path.Combine(Directory.GetCurrentDirectory(), "SimpleBus");
		Directory.CreateDirectory(Path.Combine(_basePath, "Queues"));
		Directory.CreateDirectory(Path.Combine(_basePath, "Exchanges"));
	}

	public bool IsModel => true;
	public bool IsConnected => true;
	public Action Shutdown { get; set; }

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		Shutdown?.Invoke();
	}

	#region Вспомогательные методы работы с файловой системой

	private string GetQueuePath(string queueName) => Path.Combine(_basePath, "Queues", queueName);
	private string GetExchangePath(string exchangeName) => Path.Combine(_basePath, "Exchanges", exchangeName);
	private string GetExchangeBindingsPath(string exchangeName) => Path.Combine(GetExchangePath(exchangeName), "bindings.json");

	private void EnsureQueue(string queueName)
	{
		var path = GetQueuePath(queueName);
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
			var config = new { Durable = true };
			var json = JsonSerializer.Serialize(config);
			File.WriteAllText(Path.Combine(path, "config.json"), json);
		}
	}

	private void EnsureExchange(string exchangeName)
	{
		var path = GetExchangePath(exchangeName);
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
			var config = new { Type = "topic", Durable = true };
			var json = JsonSerializer.Serialize(config);
			File.WriteAllText(Path.Combine(path, "config.json"), json);
			File.WriteAllText(Path.Combine(path, "bindings.json"), "[]");
		}
	}

	private List<Binding> GetBindings(string exchangeName)
	{
		var path = GetExchangeBindingsPath(exchangeName);
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
		var path = GetExchangeBindingsPath(exchangeName);
		var json = JsonSerializer.Serialize(bindings, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(path, json);
	}

	private bool MatchesTopic(string topic, string pattern)
	{
		if (pattern == "#") return true;
		if (pattern == "*") return true; // упрощённо
		return topic == pattern;
	}

	private void SaveMessage<TMessage>(string queueName, TMessage message, int? ttl)
	{
		var queuePath = GetQueuePath(queueName);
		var envelope = new MessageEnvelope<TMessage>
		{
			Body = message,
			CreatedAt = DateTime.UtcNow,
			TtlSeconds = ttl
		};
		var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true });
		var fileName = $"{Guid.NewGuid():N}.json";
		var filePath = Path.Combine(queuePath, fileName);
		File.WriteAllText(filePath, json);
	}

	private List<TMessage> GetMessages<TMessage>(string queueName, int? count = null)
	{
		var queuePath = GetQueuePath(queueName);
		if (!Directory.Exists(queuePath)) return new List<TMessage>();

		var files = Directory.GetFiles(queuePath, "*.json");
		var envelopes = new List<MessageEnvelope<TMessage>>();

		foreach (var file in files)
		{
			try
			{
				var json = File.ReadAllText(file);
				var envelope = JsonSerializer.Deserialize<MessageEnvelope<TMessage>>(json);
				if (envelope != null)
				{
					if (envelope.ExpiresAt.HasValue && envelope.ExpiresAt.Value < DateTime.UtcNow)
					{
						File.Delete(file);
						continue;
					}
					envelopes.Add(envelope);
				}
			}
			catch { /* игнорируем битые файлы */ }
		}

		var result = envelopes.OrderBy(e => e.CreatedAt).Select(e => e.Body).ToList();
		if (count.HasValue && count.Value > 0 && result.Count > count.Value)
			result = result.Take(count.Value).ToList();
		return result;
	}

	private void InvokeSubscribers<TMessage>(string queueName, TMessage message)
	{
		if (_subscribers.TryGetValue(queueName, out var list))
		{
			lock (list)
			{
				foreach (var action in list)
				{
					try { action(message); }
					catch { /* обработчик не должен ломать шину */ }
				}
			}
		}
	}

	#endregion

	#region Реализация ISimpleBus

	public void Send<TMessage>(TMessage message, int? ttl = null) where TMessage : class
	{
		var defaultQueue = typeof(TMessage).Name;
		Send(defaultQueue, message, ttl);
	}

	public void Send<TMessage>(string queue, TMessage message, int? ttl = null) where TMessage : class
	{
		EnsureQueue(queue);
		SaveMessage(queue, message, ttl);
		InvokeSubscribers(queue, message);
	}

	public void SendSelf<TMessage>(TMessage message) where TMessage : class
	{
		Send(message, null);
	}

	public void Publish<TMessage>(TMessage message) where TMessage : class
	{
		Publish(message, "", "default");
	}

	public void Publish<TMessage>(TMessage message, string topic, string exchangeName = null) where TMessage : class
	{
		if (string.IsNullOrEmpty(exchangeName)) exchangeName = "default";
		EnsureExchange(exchangeName);

		var bindings = GetBindings(exchangeName);
		foreach (var binding in bindings)
		{
			if (MatchesTopic(topic, binding.TopicPattern))
			{
				EnsureQueue(binding.QueueName);
				SaveMessage(binding.QueueName, message, null);
				InvokeSubscribers(binding.QueueName, message);
			}
		}
	}

	public void Subscribe<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
	{
		EnsureQueue(queueName);
		var list = _subscribers.GetOrAdd(queueName, _ => new List<Action<object>>());
		lock (list)
		{
			list.Add(msg => action((TMessage)msg));
		}
	}

	public void SubscribeExchange<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
	{
		Subscribe(queueName, action, messageHandlerSettings);

		string defaultExchange = "default";
		EnsureExchange(defaultExchange);
		var bindings = GetBindings(defaultExchange);
		if (!bindings.Any(b => b.QueueName == queueName && b.TopicPattern == "#"))
		{
			bindings.Add(new Binding { QueueName = queueName, TopicPattern = "#" });
			SaveBindings(defaultExchange, bindings);
		}
	}

	public List<TMessage> GetMessageExchange<TMessage>(string queueName)
	{
		return GetMessages<TMessage>(queueName);
	}

	public List<TMessage> GetMessageExchange<TMessage>(string queueName, int count)
	{
		return GetMessages<TMessage>(queueName, count);
	}

	public void Delete<TMessage>(string queue) where TMessage : class
	{
		Delete(queue);
	}

	public void Delete<TMessage>() where TMessage : class
	{
		Delete(typeof(TMessage).Name);
	}

	public void Delete(string queue)
	{
		var queuePath = GetQueuePath(queue);
		if (Directory.Exists(queuePath))
			Directory.Delete(queuePath, true);

		_subscribers.TryRemove(queue, out _);

		var exchangesDir = Path.Combine(_basePath, "Exchanges");
		if (Directory.Exists(exchangesDir))
		{
			foreach (var exchangeDir in Directory.GetDirectories(exchangesDir))
			{
				var exchangeName = Path.GetFileName(exchangeDir);
				var bindings = GetBindings(exchangeName);
				var newBindings = bindings.Where(b => b.QueueName != queue).ToList();
				if (newBindings.Count != bindings.Count)
					SaveBindings(exchangeName, newBindings);
			}
		}
	}

	public void Purge(string queueName)
	{
		var queuePath = GetQueuePath(queueName);
		if (Directory.Exists(queuePath))
		{
			foreach (var file in Directory.GetFiles(queuePath, "*.json"))
			{
				try { File.Delete(file); }
				catch { /* игнорируем */ }
			}
		}
	}

	public void Reconnect(string name = null)
	{
		// В файловой реализации ничего не делаем
	}

	public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response) where TRequest : class where TResponse : class
	{
		return Respond(response, null);
	}

	public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName) where TRequest : class where TResponse : class
	{
		string requestQueue = queueName ?? typeof(TRequest).Name;
		string responseQueue = requestQueue + ".responses";

		Action<object> handler = obj =>
		{
			try
			{
				var req = (TRequest)obj;
				var resp = response(req);
				Send(responseQueue, resp);
			}
			catch { /* ошибка в обработчике */ }
		};

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

	#endregion

	#region Вспомогательные классы

	private class MessageEnvelope<T>
	{
		public T Body { get; set; }
		public DateTime CreatedAt { get; set; }
		public int? TtlSeconds { get; set; }
		public DateTime? ExpiresAt => TtlSeconds.HasValue ? CreatedAt.AddSeconds(TtlSeconds.Value) : (DateTime?)null;
	}

	private class Binding
	{
		public string QueueName { get; set; }
		public string TopicPattern { get; set; }
	}

	private class DisposableAction : IDisposable
	{
		private Action _disposeAction;
		public DisposableAction(Action action) => _disposeAction = action;
		public void Dispose()
		{
			_disposeAction?.Invoke();
			_disposeAction = null;
		}
	}

	#endregion
}