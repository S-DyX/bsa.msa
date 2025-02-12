using Bsa.Msa.Common.Services.Impl;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using System.Collections.Concurrent;

namespace Bsa.Msa.RabbitMq.Core
{
    public class LocalBus : ILocalBus
    {
        private readonly IMessageHandlerFactory _factory;

        private readonly ConcurrentDictionary<string, IMessageHandlerSettings> _handlerSettings = new ConcurrentDictionary<string, IMessageHandlerSettings>();
        private readonly ConcurrentDictionary<string, object> _handlers = new ConcurrentDictionary<string, object>();

        public LocalBus(IMessageHandlerFactory factory)
        {
            _factory = factory;
        }

        protected LocalBus(ILocalContainer container)
        : this(new MessageHandlerFactory(new HandlerRegistry(), container))
        {
        }

        public void Register(IMessageHandlerSettings settings)
        {
            if (!_handlerSettings.ContainsKey(settings.SubscriptionEndpoint))
            {
                _handlerSettings.TryAdd(settings.SubscriptionEndpoint, settings);
            }
        }


        public bool Handle<TMessage>(string subscriptionEndpoint, TMessage message)
        {
            if (string.IsNullOrEmpty(subscriptionEndpoint))
                return false;

            if (_handlerSettings.ContainsKey(subscriptionEndpoint))
            {
                var messageHandlerSettings = _handlerSettings[subscriptionEndpoint];
                IMessageHandler<TMessage> messageHandler = null;
                IMessageHandlerAsync<TMessage> messageHandlerAsync = null;

                object handler;
                if (_handlers.TryGetValue(subscriptionEndpoint, out handler))
                {
                    messageHandler = handler as IMessageHandler<TMessage>;
                    messageHandlerAsync = handler as IMessageHandlerAsync<TMessage>;
                }
                if (messageHandler == null)
                {
                    messageHandler = _factory.Create<TMessage>(messageHandlerSettings.Type, messageHandlerSettings, null, null) as IMessageHandler<TMessage>;
                    _handlers.TryAdd(subscriptionEndpoint, messageHandler);
                    messageHandler?.Handle(message);
                    return true;
                }
                if (messageHandlerAsync == null)
                {
                    messageHandlerAsync = _factory.Create<TMessage>(messageHandlerSettings.Type, messageHandlerSettings, null, null) as IMessageHandlerAsync<TMessage>;
                    _handlers.TryAdd(subscriptionEndpoint, messageHandler);
                    messageHandlerAsync?.HandleAsync(message).ConfigureAwait(false);
                    return true;
                }


            }
            return false;
        }
    }
}
