using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Settings;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Bsa.Msa.Common.Services.Impl
{
	/// <inheritdoc />
	public sealed class CommandFactory : ICommandFactory
	{
		private readonly ICommandRegistry commandRegistry;
		private readonly ILocalLogger _localLogger;
		private readonly ILocalContainer localContainer;

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="commandRegistry"></param>
		/// <param name="localLogger"></param>
		/// <param name="localContainer"></param>
		public CommandFactory(ICommandRegistry commandRegistry, ILocalLogger localLogger = null, ILocalContainer localContainer = null)
		{
			this.commandRegistry = commandRegistry;
			_localLogger = localLogger;
			this.localContainer = localContainer;
		}

		/// <inheritdoc />
		public ICommand Create(string commandType, ISettings settings, CancellationToken cancellationToken)
		{
			Type handlerType = null;
			try
			{
				handlerType = commandRegistry.Resolve(commandType);
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
				var instance = Activator.CreateInstance(handlerType, args) as ICommand;
				return instance;
			}
			catch (Exception e)
			{
				_localLogger.Error($"Can not create instance of command type:{commandType}; TypeOf:{handlerType}; Message:{e.Message}", e);
				throw;
			}



		}
	}
}
