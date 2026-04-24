using System;
using System.Collections.Generic;
using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	/// <summary>
	/// Simple connection bus 
	/// </summary>
	public interface ISimpleBus : IDisposable
	{
		/// <summary>
		/// Model is init
		/// </summary>
		bool IsModel { get; }

		/// <summary>
		/// Connection is open
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Subscribe to queue
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="queueName"></param>
		/// <param name="action"></param>
		/// <param name="messageHandlerSettings"></param>
		void Subscribe<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings);

		/// <summary>
		/// Service is Shutdown
		/// </summary>
		Action Shutdown { get; set; }

		/// <summary>
		/// Subscribe to Exchange
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="queueName"></param>
		/// <param name="action"></param>
		/// <param name="messageHandlerSettings"></param>
		void SubscribeExchange<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings);

		void SendSelf<TMessage>(TMessage message) where TMessage : class;

		/// <summary>
		/// Send message
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="message"></param>
		void Send<TMessage>(TMessage message) where TMessage : class;

		/// <summary>
		/// Send message to queue
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="queue"></param>
		/// <param name="message"></param>
		void Send<TMessage>(string queue, TMessage message) where TMessage : class;

		/// <summary>
		/// Publish to exchange
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="message"></param>
		void Publish<TMessage>(TMessage message) where TMessage : class;

		/// <summary>
		/// Publish to exchange
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="message"></param>
		/// <param name="topic"></param>
		/// <param name="exchangeName"></param>
		void Publish<TMessage>(TMessage message, string topic, string exchangeName = null) where TMessage : class;

		/// <summary>
		/// Delete queue
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="queue"></param>
		void Delete<TMessage>(string queue) where TMessage : class;

		/// <summary>
		/// Delete exchange
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		void Delete<TMessage>() where TMessage : class;

		/// <summary>
		/// Delete queue
		/// </summary>
		/// <param name="queue"></param>
		void Delete(string queue);

		/// <summary>
		/// Get messages from queue
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="queueName"></param>
		/// <returns></returns>
		List<TMessage> GetMessageExchange<TMessage>(string queueName);

		/// <summary>
		/// Get messages from queue
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="queueName"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		List<TMessage> GetMessageExchange<TMessage>(string queueName, int count);

	
		/// <summary>
		/// Does not work
		/// </summary>
		/// <typeparam name="TRequest"></typeparam>
		/// <typeparam name="TResponse"></typeparam>
		/// <param name="response"></param>
		/// <returns></returns>
		IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
			where TRequest : class
			where TResponse : class;

		/// <summary>
		/// Does not work
		/// </summary>
		/// <typeparam name="TRequest"></typeparam>
		/// <typeparam name="TResponse"></typeparam>
		/// <param name="response"></param>
		/// <param name="queueName"></param>
		/// <returns></returns>
		IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class;


		/// <summary>
		/// Try to reconnect
		/// </summary>
		/// <param name="name"></param>
		void Reconnect(string? name = null);
	}
}

