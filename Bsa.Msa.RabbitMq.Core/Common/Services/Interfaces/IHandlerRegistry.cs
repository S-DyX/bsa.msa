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
		// Handler name -> (message type, handler type).
		private readonly TypeNameMap<Tuple<Type, Type>> _types = new TypeNameMap<Tuple<Type, Type>>();

		public HandlerRegistry()
		{
			var handlerTypes = AssemblyHelpers.GetTypes(IsHandler);
			foreach (var handlerType in handlerTypes)
			{
				var messageType = GetMessageType(handlerType);
				_types.Add(handlerType, Tuple.Create(messageType, handlerType));
			}
		}

		public void Register<TMessage, TType>(string name) where TType : IMessageHandler<TMessage>
		{
			_types.Set(name, Tuple.Create(typeof(TMessage), typeof(TType)));
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
			return _types.Find(name)?.Item2;
		}

		public Type ResolveMessage(string name)
		{
			return _types.Find(name)?.Item1;
		}

		private static bool IsHandler(Type type)
		{
			return type.IsClass
				&& !type.IsAbstract
				&& !type.ContainsGenericParameters
				&& GetMessageType(type) != null;
		}

		/// <summary>
		/// The message is the first generic argument of IMessageHandler&lt;T&gt;,
		/// IMessageHandlerAsync&lt;T&gt; or IMessageHandler&lt;T, TResponse&gt;
		/// (before, the last argument was taken, i.e. the response type).
		/// </summary>
		private static Type GetMessageType(Type handlerType)
		{
			foreach (var i in handlerType.GetInterfaces())
			{
				if (!i.IsGenericType) continue;

				var definition = i.GetGenericTypeDefinition();
				if (definition == typeof(IMessageHandler<>)
					|| definition == typeof(IMessageHandlerAsync<>)
					|| definition == typeof(IMessageHandler<,>))
					return i.GetGenericArguments()[0];
			}

			return null;
		}
	}
}
