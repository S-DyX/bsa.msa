using System;
using Bsa.Msa.Common;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;

namespace Bsa.Msa.RabbitMq.Core
{
	public sealed class SubscriberFactory : ISubscriberFactory
	{
		private readonly ILocalLogger _localLogger;
		private readonly IRabbitMqSettings _settings;
		private readonly ILocalBus _localBus;
		private readonly IHandlerRegistry _subscriptionRegistry;

		public SubscriberFactory(ILocalLogger localLogger, IRabbitMqSettings settings, ILocalBus localBus, IHandlerRegistry subscriptionRegistry)
		{
			this._localLogger = localLogger;
			_settings = settings;
			_localBus = localBus;
			this._subscriptionRegistry = subscriptionRegistry;
		}
		public ISubscriber Create(string name, IMessageHandlerSettings messageHandlerSettings, IMessageHandlerFactory messageHandlerFactory)
		{
			Type genericType = typeof(SubscriberBase<>);
			var type = _subscriptionRegistry.ResolveMessage(name);
			if (type == null)
				throw new InvalidOperationException($"Handler not found {name}");
			Type constructedClass = genericType.MakeGenericType(type);

			var simpleConnection = new SimpleConnection(_settings); 
			var simpleBus = new SimpleBus(simpleConnection);
			return Activator.CreateInstance(constructedClass, messageHandlerSettings, messageHandlerFactory, _localLogger, simpleBus, _localBus) as ISubscriber;
		}
	}
}
