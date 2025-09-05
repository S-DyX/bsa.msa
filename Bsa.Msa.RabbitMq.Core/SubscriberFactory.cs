using Bsa.Msa.Common;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;
using System;

namespace Bsa.Msa.RabbitMq.Core
{
	public sealed class SubscriberFactory : ISubscriberFactory
	{
		private readonly ILocalLogger _localLogger;
		private readonly ISimpleBusNaming _busNaming;
		private readonly IRabbitMqSettings _settings;
		private readonly ILocalBus _localBus;
		private readonly IHandlerRegistry _subscriptionRegistry;
		private readonly ISerializeService _serializeService;

		public SubscriberFactory(IRabbitMqSettings settings, ILocalBus localBus, IHandlerRegistry subscriptionRegistry, ILocalLogger localLogger = null,
			ISerializeService serializeService = null,  ISimpleBusNaming busNaming = null)
		{
			this._localLogger = localLogger;
			_busNaming = busNaming ?? new DefaultSimpleBusNaming();
			_settings = settings;
			_localBus = localBus;
			this._subscriptionRegistry = subscriptionRegistry;
			_serializeService = serializeService ?? new SerializeService();

		}
		public ISubscriber Create(string name, IMessageHandlerSettings messageHandlerSettings, IMessageHandlerFactory messageHandlerFactory)
		{
			Type genericType = typeof(SubscriberBase<>);
			var type = _subscriptionRegistry.ResolveMessage(name);
			if (type == null)
				throw new InvalidOperationException($"Handler not found {name}");
			Type constructedClass = genericType.MakeGenericType(type);

			var simpleConnection = new SimpleConnection(_settings, _localLogger);
			var simpleBus = new SimpleBus(simpleConnection, _localLogger, _serializeService, _busNaming);
			return Activator.CreateInstance(constructedClass, messageHandlerSettings, messageHandlerFactory, _localLogger, simpleBus, _localBus, _busNaming) as ISubscriber;
		}
	}
}
