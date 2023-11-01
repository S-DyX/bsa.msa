using Bsa.Msa.Common;
using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	public static class BusManagerExtension
	{
		public static void SendBack<TMessage>(this IBusManager bus, IMessageHandlerSettings settings, TMessage message) where TMessage : class
		{
			bus.Send(settings.SubscriptionEndpoint, message, true);
		}

		public static bool SendBackIfLock<TMessage>(this IBusManager bus,string key ,IMessageHandlerSettings settings, TMessage message) where TMessage : class
		{
			if (ProcessKeyLock.Instance.IsLock(key))
			{
				bus.SendBack(settings, message);
				return true;
			}
			return false;
		}
	}
}
