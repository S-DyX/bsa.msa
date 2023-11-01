using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Settings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Bsa.Msa.Common.Services.Impl
{
	public sealed class CommandFactory : ICommandFactory
	{
		private readonly ICommandRegistry commandRegistry;
		private readonly ILocalContainer localContainer;

		public CommandFactory(ICommandRegistry commandRegistry, ILocalContainer localContainer)
		{
			this.commandRegistry = commandRegistry;
			this.localContainer = localContainer;
		}
		public ICommand Create(string commandType, ISettings settings, CancellationToken cancellationToken)
		{
			var handlerType = commandRegistry.Resolve(commandType);
			if (handlerType == null)
				throw new InvalidOperationException($"Type not found {commandType}");
			var constructor = handlerType.GetConstructor(Type.EmptyTypes);
			if (constructor != null)
				return Activator.CreateInstance(handlerType) as ICommand;

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
							throw new InvalidOperationException($"Parameter not found {p.ParameterType.Name} {p.Name}");
						}
					}
				}

			}
			object[] args = result.ToArray();
			return Activator.CreateInstance(handlerType, args) as ICommand;


		}
	}
}
