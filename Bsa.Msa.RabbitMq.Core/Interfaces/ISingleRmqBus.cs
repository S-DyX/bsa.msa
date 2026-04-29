using System;
using System.Collections.Generic;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	/// <summary>
	/// Single instance of <see cref="IBusManager"/>
	/// </summary>
	public interface ISingleRmqBus : IBusManager
	{
	}

	/// <inheritdoc />
	public sealed class SingleRmqBus : ISingleRmqBus
	{
		private readonly IBusManager _busManager;

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="busManager"></param>
		public SingleRmqBus(IBusManager busManager)
		{
			_busManager = busManager;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			using (_busManager)
			{

			}
		}

		/// <inheritdoc />
		public void Send<TMessage>(TMessage message, int? ttl = null) where TMessage : class
		{
			_busManager.Send(message, ttl);
		}

		/// <inheritdoc />
		public void Send<TMessage>(string queue, TMessage message, int? ttl = null, bool forceSend = false) where TMessage : class
		{
			_busManager.Send(queue, message, ttl, forceSend);
		}

		/// <inheritdoc />
		public void Publish<TMessage>(TMessage message) where TMessage : class
		{
			_busManager.Publish(message);
		}

		/// <inheritdoc />
		public void Delete<TMessage>(string queue) where TMessage : class
		{
			_busManager.Delete<TMessage>(queue);
		}

		/// <inheritdoc />
		public void Delete<TMessage>() where TMessage : class
		{
			_busManager.Delete<TMessage>();
		}

		public void Delete(string queue)
		{
			_busManager.Delete(queue);
		}

		/// <inheritdoc />
		public void Publish<TMessage>(TMessage message, string topic,  string exchangeName = null) where TMessage : class
		{
			_busManager.Publish(message, topic, exchangeName);
		}

		/// <inheritdoc />
		public List<TMessage> GetMessageExchange<TMessage>(string queueName)
		{
			return _busManager.GetMessageExchange<TMessage>(queueName);
		}

		public List<TMessage> GetMessageExchange<TMessage>(string queueName, int count)
		{
			return _busManager.GetMessageExchange<TMessage>(queueName, count);
		}

		/// <inheritdoc />
		public TResponse Request<TMessage, TResponse>(TMessage message) where TMessage : class where TResponse : class
		{
			throw new NotImplementedException();
			//return _busManager.Request<TMessage, TResponse>(message);
		}


		/// <inheritdoc />
		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response) where TRequest : class where TResponse : class
		{
			return _busManager.Respond<TRequest, TResponse>(response);
		}

		/// <inheritdoc />
		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName) where TRequest : class where TResponse : class
		{
			return _busManager.Respond<TRequest, TResponse>(response, queueName);
		}
	}
}
