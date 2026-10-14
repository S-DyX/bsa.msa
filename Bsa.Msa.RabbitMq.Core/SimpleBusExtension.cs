using System;
using System.Linq;

namespace Bsa.Msa.RabbitMq.Core
{
	public static class SimpleBusExtension
	{
		public static string GetQueueName<TMessage>()
		{
			var type = typeof(TMessage);
			return GetQueueName(type);
		}

		public static string GetQueueName(Type type)
		{
			return $"{GetTypeName(type)}:{type.Assembly.GetName().Name}";
		}

		/// <summary>
		/// Returns the type name without assembly versions, e.g.
		/// <c>Ns.Message[Ns.Item]</c> for <c>Ns.Message&lt;Ns.Item&gt;</c>.
		/// </summary>
		/// <remarks>
		/// <see cref="Type.FullName"/> of a generic type contains the assembly
		/// versions of its arguments: the queue name changed with every
		/// version and could exceed the 255 characters allowed by RabbitMQ.
		/// </remarks>
		/// <param name="type">Message type.</param>
		/// <returns>Readable type name.</returns>
		public static string GetTypeName(Type type)
		{
			ArgumentNullException.ThrowIfNull(type);

			if (!type.IsGenericType || type.ContainsGenericParameters)
				return type.FullName ?? type.Name;

			var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
			var index = name.IndexOf('`');
			if (index > 0)
				name = name.Remove(index);

			var arguments = type.GetGenericArguments().Select(GetTypeName);
			return $"{name}[{string.Join(",", arguments)}]";
		}

		public static string GetExchangeName<TMessage>()
		{
			var queueName = $"Exchange:{GetQueueName<TMessage>()}";
			return queueName;
		}
	}
}
