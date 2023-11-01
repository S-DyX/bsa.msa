using Bsa.Msa.Common.Settings;
namespace Bsa.Msa.Common.Services.MessageHandling
{
	public interface IMessageHandlerFactory
	{
		IMessageHandler<TMessage> Create<TMessage>(string type, ISettings settings);

		IMessageHandler<TMessage, TResponse> Create<TMessage, TResponse>(string type, ISettings settings);

	}

	
}
