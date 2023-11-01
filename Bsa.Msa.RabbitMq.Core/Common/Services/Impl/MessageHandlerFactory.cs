using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bsa.Msa.Common.Services.Impl
{
	public sealed class MessageHandlerFactory : IMessageHandlerFactory
	{
		private readonly IHandlerRegistry registry;
		private readonly ILocalContainer localContainer;

		public MessageHandlerFactory(IHandlerRegistry registry, ILocalContainer localContainer)
		{
			this.registry = registry;
			this.localContainer = localContainer;
		}
		public IMessageHandler<TMessage> Create<TMessage>(string type, ISettings settings)
		{
			var handlerType = registry.ResolveHandler(type);
			if (handlerType == null)
				throw new InvalidOperationException($"Type not found {type}");
			var constructor = handlerType.GetConstructor(Type.EmptyTypes);
			if (constructor != null)
				return Activator.CreateInstance(handlerType) as IMessageHandler<TMessage>;

			var constructors = handlerType.GetConstructors();
			var result = new List<object>();
			foreach (var c in constructors)
			{
				var parameters = c.GetParameters();
				foreach (var p in parameters)
				{
					if (p.ParameterType == typeof(ISettings))
					{
						result.Add(settings);
					}
					else
					{
						var inst = localContainer.Resolve(p.ParameterType);
						if (inst != null)
						{
							result.Add(inst);
						}
						else
						{
							throw new InvalidOperationException($"type not found {p.ParameterType}");
						}
					}
				}
			}
			object[] args = result.ToArray();
			return Activator.CreateInstance(handlerType, args) as IMessageHandler<TMessage>;
		}

		public IMessageHandler<TMessage, TResponse> Create<TMessage, TResponse>(string type, ISettings settings)
		{
			throw new System.NotImplementedException();
		}


	}
}
