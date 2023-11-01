using Bsa.Msa.Common.Services.MessageHandling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Bsa.Msa.Common.Services.Interfaces
{
	public interface IHandlerRegistry
	{
		void Register<TMessage, TType>(string name) where TType : IMessageHandler<TMessage>;
		void Register<TMessage, TType>() where TType : IMessageHandler<TMessage>;

		Type ResolveHandler(string name);
		Type ResolveMessage(string name);
	}

	public sealed class HandlerRegistry : IHandlerRegistry
	{
		public HandlerRegistry()
		{
			types = new Dictionary<string, Tuple<Type, Type>>();
			var type = typeof(IMessageHandler);


			//var typesDelivered = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
			//	.Where(p => type.IsAssignableFrom(p));
			var typesList = type.GetDeliveredTypes();

			foreach (var tType in typesList.Where(p => type.IsAssignableFrom(p)))
			{
				var interfaces = tType.GetInterfaces()
					.Where(i => i.IsConstructedGenericType);
				foreach (var i in interfaces)
				{
					var gTypes = i.GetGenericArguments();
					foreach (var mType in gTypes)
					{
						types[tType.Name] = Tuple.Create(mType, tType);
					}
				}
			}
		}
		private readonly Dictionary<string, Tuple<Type, Type>> types;
		public void Register<TMessage, TType>(string name) where TType : IMessageHandler<TMessage>
		{
			var tType = typeof(TType);
			var mType = typeof(TMessage);
			types[name] = Tuple.Create(mType, tType);
		}

		public void Register<TMessage, TType>() where TType : IMessageHandler<TMessage>
		{
			var tType = typeof(TType);
			Register<TMessage, TType>(tType.Name);
		}

		public void Register<TType>()
		{
			throw new NotImplementedException();
		}

		public T Resolve<T>()
		{
			throw new NotImplementedException();
		}

		public Type ResolveHandler(string name)
		{
			if (types.ContainsKey(name))
				return types[name].Item2;
			return null;
		}
		public Type ResolveMessage(string name)
		{
			if (types.ContainsKey(name))
				return types[name].Item1;
			return null;
		}
	}
}
