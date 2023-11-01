using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	public interface ILocalBus
	{
		void Register(IMessageHandlerSettings settings);

		bool Handle<TMessage>(string subscriptionEndpoint, TMessage message);
	}

	
}
