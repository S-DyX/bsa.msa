using System;
using System.Collections.Generic;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	public interface IBusManager : IDisposable
	{
		//void Subscribe(Action<ISimpleBus> action);

		void Send<TMessage>(TMessage message) where TMessage : class;

		//void SendSelf<TMessage>(TMessage message) where TMessage : class;

		void Send<TMessage>(string queue, TMessage message, bool forceSend = false) where TMessage : class;

		void Publish<TMessage>(TMessage message) where TMessage : class;

		void Delete<TMessage>(string queue) where TMessage : class;

		void Delete<TMessage>() where TMessage : class;

		void Delete(string queue);

		void Publish<TMessage>(TMessage message, string topic, string exchangeName = null) where TMessage : class;

		List<TMessage> GetMessageExchange<TMessage>(string queueName);

		List<TMessage> GetMessageExchange<TMessage>(string queueName, int count);

	

		IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
			where TRequest : class
			where TResponse : class;

		IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class;
	}


}