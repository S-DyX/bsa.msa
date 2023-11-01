using System;
using System.Collections.Generic;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	public interface ISingleRmqBus : IBusManager
	{
	}

	public class SingleRmqBus : ISingleRmqBus
	{
		private readonly IBusManager _busManager;

		public SingleRmqBus(IBusManager busManager)
		{
			_busManager = busManager;
		}

		public void Dispose()
		{
			using (_busManager)
			{

			}
		}

		public void Send<TMessage>(TMessage message) where TMessage : class
		{
			_busManager.Send(message);
		}

		public void Send<TMessage>(string queue, TMessage message, bool forceSend = false) where TMessage : class
		{
			_busManager.Send(queue, message, forceSend);
		}

		public void Publish<TMessage>(TMessage message) where TMessage : class
		{
			_busManager.Publish(message);
		}

		public void Delete<TMessage>(string queue) where TMessage : class
		{
			_busManager.Delete<TMessage>(queue);
		}

		public void Delete<TMessage>() where TMessage : class
		{
			_busManager.Delete<TMessage>();
		}

		public void Delete(string queue)
		{
			_busManager.Delete(queue);
		}

		public void Publish<TMessage>(TMessage message, string topic, string exchangeName = null) where TMessage : class
		{
			_busManager.Publish(message, topic, exchangeName);
		}

		public List<TMessage> GetMessageExchange<TMessage>(string queueName)
		{
			return _busManager.GetMessageExchange<TMessage>(queueName);
		}

		public List<TMessage> GetMessageExchange<TMessage>(string queueName, int count)
		{
			return _busManager.GetMessageExchange<TMessage>(queueName, count);
		}

		public TResponse Request<TMessage, TResponse>(TMessage message) where TMessage : class where TResponse : class
		{
			throw new NotImplementedException();
			//return _busManager.Request<TMessage, TResponse>(message);
		}

		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response) where TRequest : class where TResponse : class
		{
			return _busManager.Respond<TRequest, TResponse>(response);
		}

		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName) where TRequest : class where TResponse : class
		{
			return _busManager.Respond<TRequest, TResponse>(response, queueName);
		}
	}
}
