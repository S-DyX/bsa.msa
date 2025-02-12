using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;

namespace Bsa.Msa.Common.Services.MessageHandling
{
	public interface IMessageHandlerFactory
	{
        IMessageHandler Create<TMessage>(string type, ISettings settings, ISimpleBus simpleBus, ILocalBus localBus);

		IMessageHandler<TMessage, TResponse> Create<TMessage, TResponse>(string type, ISettings settings, ISimpleBus simpleBus, ILocalBus localBus);

	}

	
}
