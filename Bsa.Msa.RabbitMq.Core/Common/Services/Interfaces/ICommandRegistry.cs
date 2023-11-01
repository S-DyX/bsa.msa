using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.MessageHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bsa.Msa.Common.Services.Interfaces
{
	public interface ICommandRegistry
	{
		void Register<TType>(string name) where TType : ICommand;
		void Register<TType>() where TType : ICommand;
		Type Resolve(string name);
	}

	public sealed class CommandRegistry : ICommandRegistry
	{
		public CommandRegistry()
		{
			types = new Dictionary<string, Type>();
			var type = typeof(ICommand);

			var typesDelivered = type.GetDeliveredTypes();
			foreach (var tType in typesDelivered)
			{
				types[tType.Name] = tType;
			}
		}
		private readonly Dictionary<string, Type> types;
		public void Register<TType>(string name) where TType : ICommand
		{
			var tType = typeof(TType);
			types[name] = tType;
		}

		public void Register<TType>() where TType : ICommand
		{
			var tType = typeof(TType);
			Register<TType>(tType.Name);
		}

		public Type Resolve(string name)
		{
			if (types.ContainsKey(name))
				return types[name];
			return null;
		}

	}
}
