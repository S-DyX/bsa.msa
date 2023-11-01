using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.Common.Services.Interfaces
{
	public interface ISubscriber : IServiceUnit
	{
	}

	public interface ISubscriberFactory
	{
		ISubscriber Create(string name, IMessageHandlerSettings messageHandlerSettings, IMessageHandlerFactory messageHandlerFactory);
	}

	


}
