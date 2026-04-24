using Bsa.Msa.Common;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;
using System;

namespace Bsa.Msa.RabbitMq.Core
{
	/// <inheritdoc />
	public sealed class SubscriberFactory : ISubscriberFactory
	{
		private readonly ILocalLogger _localLogger;
		private readonly ISimpleBusNaming _busNaming;
		private readonly IRabbitMqSettings _settings;
		private readonly ILocalBus _localBus;
		private readonly IHandlerRegistry _subscriptionRegistry;
		private readonly ISimpleBus _simpleBus;
		private readonly ISerializeService _serializeService;

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="localBus"></param>
		/// <param name="subscriptionRegistry"></param>
		/// <param name="simpleBus"></param>
		/// <param name="localLogger"></param>
		/// <param name="serializeService"></param>
		/// <param name="busNaming"></param>
		public SubscriberFactory(IRabbitMqSettings settings, ILocalBus localBus, IHandlerRegistry subscriptionRegistry,ISimpleBus simpleBus, ILocalLogger localLogger = null,
			ISerializeService serializeService = null,  ISimpleBusNaming busNaming = null)
		{
			this._localLogger = localLogger;
			_busNaming = busNaming ?? new DefaultSimpleBusNaming();
			_settings = settings;
			_localBus = localBus;
			this._subscriptionRegistry = subscriptionRegistry;
			_simpleBus = simpleBus;
			_serializeService = serializeService ?? new SerializeService();

		}

		/// <inheritdoc />
		public ISubscriber Create(IMessageHandlerSettings messageHandlerSettings, IMessageHandlerFactory messageHandlerFactory)
		{
			var name = messageHandlerSettings.Type;
			Type genericType = typeof(SubscriberBase<>);
			var type = _subscriptionRegistry.ResolveMessage(name);
			if (type == null)
				throw new InvalidOperationException($"Handler not found {name}");
			Type constructedClass = genericType.MakeGenericType(type);

			var simpleConnection = new SimpleConnection(_settings, _localLogger);
			var simpleBus = new SimpleBus(simpleConnection, _localLogger, _serializeService, _busNaming);
			return Activator.CreateInstance(constructedClass, messageHandlerSettings, messageHandlerFactory, _localLogger, simpleBus, _localBus, _busNaming) as ISubscriber;
		}

		/// <inheritdoc />
		public void Delete(IMessageHandlerSettings messageHandlerSettings)
		{
			var name = messageHandlerSettings.Type;
			Type genericType = typeof(SubscriberBase<>);
			var type = _subscriptionRegistry.ResolveMessage(name);
			if (type == null)
				throw new InvalidOperationException($"Handler not found {name}");
			Type constructedClass = genericType.MakeGenericType(type);
			var subscriptionEndpoint = string.IsNullOrEmpty(messageHandlerSettings.SubscriptionEndpoint)
				? _busNaming.GetQueueName(type)
				: messageHandlerSettings.SubscriptionEndpoint;
			_simpleBus.Delete(subscriptionEndpoint);
			_simpleBus.Delete($"{subscriptionEndpoint}.Error");
			//if (messageHandlerSettings.UseExchange)
			//{
			//	_simpleBus.Delete<>();
			//}
		}
	}
}
