using Bsa.Msa.RabbitMq.Core.Interfaces;
using System;

namespace Bsa.Msa.RabbitMq.Core
{
	/// <inheritdoc />
	internal sealed class DefaultSimpleBusNaming : ISimpleBusNaming
	{
		/// <inheritdoc />
		public string GetQueueName(Type type)
		{
			return SimpleBusExtension.GetQueueName(type);
		}

		/// <inheritdoc />
		public string GetQueueName<TMessage>()
		{
			return SimpleBusExtension.GetQueueName<TMessage>();
		}
		/// <inheritdoc />
		public string GetExchangeName<TMessage>()
		{
			return SimpleBusExtension.GetExchangeName<TMessage>();
		}
	}

	/// <inheritdoc />
	public sealed class EasyNetQSimpleBusNaming : ISimpleBusNaming
	{
		/// <inheritdoc />
		public string GetQueueName<TMessage>()
		{
			var type = typeof(TMessage);
			return GetQueueName(type);
		}
		/// <inheritdoc />
		public string GetQueueName(Type type)
		{
			var name = SimpleBusExtension.GetTypeName(type);
			return $"{name}, {type.Assembly.GetName().Name}";
		}

		/// <inheritdoc />
		public string GetExchangeName<TMessage>()
		{
			var queueName = $"{GetQueueName<TMessage>()}";
			return queueName;
		}
	}
}
