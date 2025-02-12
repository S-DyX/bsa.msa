namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	public interface ISimpleBusNaming
	{
		string GetQueueName<TMessage>();
		string GetExchangeName<TMessage>();
	}
}
