using System;

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
			var fullName = type.FullName;
			if (type.IsGenericType && !string.IsNullOrEmpty(fullName))
			{
				int index = fullName.IndexOf('`');
				if (index > 0)
				{
					fullName.Remove(index);
				}
			}

			return $"{fullName}:{type.Assembly.GetName().Name}";
		}

		public static string GetExchangeName<TMessage>()
		{
			var queueName = $"Exchange:{GetQueueName<TMessage>()}";
			return queueName;
		}
	}
}
