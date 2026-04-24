using System;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	/// <summary>
	/// The service is responsible for naming queues and exchangers. 
	/// </summary>
	public interface ISimpleBusNaming
	{
		/// <summary>
		/// Queue name from  type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		string GetQueueName(Type type);
		/// <summary>
		/// Queue name from message type
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <returns></returns>
		string GetQueueName<TMessage>();

		/// <summary>
		/// Queue name for exchange from message type
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <returns></returns>
		string GetExchangeName<TMessage>();
	}
}
