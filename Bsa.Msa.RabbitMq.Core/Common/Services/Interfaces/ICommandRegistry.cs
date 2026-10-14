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
		private readonly TypeNameMap<Type> _types = new TypeNameMap<Type>();

		public CommandRegistry()
		{
			var commandType = typeof(ICommand);
			var commandTypes = AssemblyHelpers.GetTypes(x => x.IsClass
				&& !x.IsAbstract
				&& !x.ContainsGenericParameters
				&& commandType.IsAssignableFrom(x));
			foreach (var type in commandTypes)
			{
				_types.Add(type, type);
			}
		}

		public void Register<TType>(string name) where TType : ICommand
		{
			_types.Set(name, typeof(TType));
		}

		public void Register<TType>() where TType : ICommand
		{
			var tType = typeof(TType);
			Register<TType>(tType.Name);
		}

		public Type Resolve(string name)
		{
			return _types.Find(name);
		}

	}
}
