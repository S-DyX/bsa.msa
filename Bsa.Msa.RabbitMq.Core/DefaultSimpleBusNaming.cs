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
			var fullName = type.FullName;
			if (type.IsGenericType && !string.IsNullOrEmpty(fullName))
			{
				int index = fullName.IndexOf('`');
				if (index > 0)
				{
					fullName.Remove(index);
				}
			}

			return $"{fullName}, {type.Assembly.GetName().Name}";
		}

		/// <inheritdoc />
		public string GetExchangeName<TMessage>()
		{
			var queueName = $"{GetQueueName<TMessage>()}";
			return queueName;
		}
	}
}
