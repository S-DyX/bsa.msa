using System;
using System.Collections.Generic;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core;
using Bsa.Msa.RabbitMq.Core.Interfaces;

namespace Bsa.Msa.Common.Services.Impl
{
	public sealed class MessageHandlerFactory : IMessageHandlerFactory
	{
		private readonly IHandlerRegistry registry;
		private readonly ILocalContainer _localContainer;
		private readonly ILocalLogger _localLogger;

		public MessageHandlerFactory(IHandlerRegistry registry, ILocalContainer localContainer, ILocalLogger localLogger = null)
		{
			this.registry = registry;
			this._localContainer = localContainer;
			_localLogger = localLogger;
		}
		public IMessageHandler Create<TMessage>(string type, ISettings settings, ISimpleBus simpleBus, ILocalBus localBus)
		{
			var handlerType = registry.ResolveHandler(type);
			if (handlerType == null)
				throw new InvalidOperationException($"Type not found {type}");
			var constructor = handlerType.GetConstructor(Type.EmptyTypes);
			if (constructor != null)
				return Activator.CreateInstance(handlerType) as IMessageHandler;

			var constructors = handlerType.GetConstructors();
			var result = new List<object>();
			foreach (var c in constructors)
			{
				var parameters = c.GetParameters();
				foreach (var p in parameters)
				{
					if (simpleBus != null && localBus != null && p.ParameterType == typeof(ISingleRmqBus))
					{
							result.Add(new SingleRmqBus(new BusManager(simpleBus, localBus)));
					}
					else if (simpleBus != null && localBus != null && p.ParameterType == typeof(IBusManager))
					{
						result.Add(new BusManager(simpleBus, localBus));
					}
					else if (p.ParameterType == typeof(ISettings))
					{
						result.Add(settings);
					}
					else
					{
						try
						{
							var inst = _localContainer.Resolve(p.ParameterType);
							if (inst != null)
							{
								result.Add(inst);
							}
							else
							{
								throw new InvalidOperationException($"type not found {p.ParameterType}");
							}
						}
						catch (Exception e)
						{
							_localLogger?.Error($"Type can not be resolved:{p.ParameterType};Message:{e.Message}", e);
							throw;
						}
						
					}
				}
			}
			object[] args = result.ToArray();
			return Activator.CreateInstance(handlerType, args) as IMessageHandler;
		}

		public IMessageHandler<TMessage, TResponse> Create<TMessage, TResponse>(string type, ISettings settings, ISimpleBus simpleBus, ILocalBus localBus)
		{
			throw new System.NotImplementedException();
		}


	}
}
