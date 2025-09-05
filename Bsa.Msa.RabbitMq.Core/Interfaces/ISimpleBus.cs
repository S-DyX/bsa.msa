using System;
using System.Collections.Generic;
using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	public interface ISimpleBus : IDisposable
	{
		//void Subscribe<TMessage>(Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings);
		bool IsModel { get; }
		bool IsConnected { get; }

		void Subscribe<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings);

		void SubscribeExchange<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings);

		void SendSelf<TMessage>(TMessage message) where TMessage : class;

		void Send<TMessage>(TMessage message) where TMessage : class;

		void Send<TMessage>(string queue, TMessage message) where TMessage : class;

		void Publish<TMessage>(TMessage message) where TMessage : class;

		void Publish<TMessage>(TMessage message, string topic, string exchangeName = null) where TMessage : class;

		void Delete<TMessage>(string queue) where TMessage : class;

		void Delete<TMessage>() where TMessage : class;
		void Delete(string queue);

		List<TMessage> GetMessageExchange<TMessage>(string queueName);

		List<TMessage> GetMessageExchange<TMessage>(string queueName, int count);

	

		IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
			where TRequest : class
			where TResponse : class;
		IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class;


		void Reconnect();
	}
}

