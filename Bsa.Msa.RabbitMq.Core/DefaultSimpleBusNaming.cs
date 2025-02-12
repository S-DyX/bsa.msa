using Bsa.Msa.RabbitMq.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bsa.Msa.RabbitMq.Core
{
	internal sealed class DefaultSimpleBusNaming : ISimpleBusNaming
	{
		public string GetQueueName<TMessage>()
		{
			return SimpleBusExtension.GetQueueName<TMessage>();
		}

		public string GetExchangeName<TMessage>()
		{
			return SimpleBusExtension.GetExchangeName<TMessage>();
		}
	}
	public sealed class EasyNetQSimpleBusNaming : ISimpleBusNaming
	{
		public string GetQueueName<TMessage>()
		{
			var type = typeof(TMessage);
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

		public string GetExchangeName<TMessage>()
		{
			var queueName = $"{GetQueueName<TMessage>()}";
			return queueName;
		}
	}
}
