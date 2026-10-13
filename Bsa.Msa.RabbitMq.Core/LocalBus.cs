using Bsa.Msa.Common.Services.Impl;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using System;
using System.Collections.Concurrent;

namespace Bsa.Msa.RabbitMq.Core
{
	/// <inheritdoc />
	public sealed class LocalBus : ILocalBus
	{
		private readonly IMessageHandlerFactory _factory;

		private readonly ConcurrentDictionary<string, IMessageHandlerSettings> _handlerSettings = new ConcurrentDictionary<string, IMessageHandlerSettings>();
		private readonly ConcurrentDictionary<string, Lazy<IMessageHandler>> _handlers =
			new ConcurrentDictionary<string, Lazy<IMessageHandler>>();

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="factory"></param>
		public LocalBus(IMessageHandlerFactory factory)
		{
			_factory = factory;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="container"></param>
		protected LocalBus(ILocalContainer container)
		: this(new MessageHandlerFactory(new HandlerRegistry(), container))
		{
		}

		/// <inheritdoc />
		public void Register(IMessageHandlerSettings settings)
		{
			if (!_handlerSettings.ContainsKey(settings.SubscriptionEndpoint))
			{
				_handlerSettings.TryAdd(settings.SubscriptionEndpoint, settings);
			}
		}

		/// <inheritdoc />
		public bool Handle<TMessage>(string subscriptionEndpoint, TMessage message)
		{
			if (string.IsNullOrEmpty(subscriptionEndpoint)) return false;

			if (!_handlerSettings.TryGetValue(subscriptionEndpoint, out var settings))
				return false;

			var handler = GetHandler<TMessage>(subscriptionEndpoint, settings);
			if (handler is IMessageHandler<TMessage> syncHandler)
			{
				syncHandler.Handle(message);
				return true;
			}

			if (handler is IMessageHandlerAsync<TMessage> asyncHandler)
			{
				asyncHandler.HandleAsync(message).GetAwaiter().GetResult();
				return true;
			}

			// Unknown handler type: let the caller deliver the message through the broker.
			return false;
		}

		private IMessageHandler GetHandler<TMessage>(
			string subscriptionEndpoint,
			IMessageHandlerSettings settings)
		{
			var lazy = _handlers.GetOrAdd(
				subscriptionEndpoint,
				_ => new Lazy<IMessageHandler>(() => _factory.Create<TMessage>(
					settings.Type,
					settings,
					null,
					null)));
			try
			{
				return lazy.Value;
			}
			catch
			{
				// Lazy caches the exception, drop it so the next call retries.
				_handlers.TryRemove(subscriptionEndpoint, out _);
				throw;
			}
		}
	}
}
