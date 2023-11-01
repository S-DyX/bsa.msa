using System;
using System.Collections.Generic;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;

namespace Bsa.Msa.RabbitMq.Core
{
	public sealed class BusManager : IBusManager
	{ 
		private readonly ISimpleBus _simpleBus;
		private readonly ILocalBus _localBus;
		private readonly object _sync = new object();

		public BusManager(ISimpleBus simpleBus, ILocalBus localBus)
		{ 
			_simpleBus = simpleBus;
			_localBus = localBus;
		}
		public BusManager(ISimpleBus simpleBus)
		{ 
			_simpleBus = simpleBus;
		}

		private ISimpleBus Bus
		{
			get
			{
				if (!_simpleBus.IsConnected)
				{
					_simpleBus.Reconnect();
				}
				return _simpleBus;
			}
		}


		public void SendSelf<TMessage>(TMessage message) where TMessage : class
		{
			Bus.SendSelf(message);
		}

		public void Send<TMessage>(string queue, TMessage message, bool forceSend) where TMessage : class
		{
			try
			{
				if (string.IsNullOrEmpty(queue))
				{
					Bus.Publish<TMessage>(message);
				}
				else
				{
					if (forceSend || _localBus == null || !_localBus.Handle(queue, message))
						Bus.Send<TMessage>(queue, message);
				}
			}
			catch (Exception)
			{
				if (string.IsNullOrEmpty(queue))
				{
					Bus.Publish<TMessage>(message);
				}
				else
				{
					Bus.Send<TMessage>(queue, message);
				}
			}
		}

		public void Publish<TMessage>(TMessage message) where TMessage : class
		{
			Bus.Publish<TMessage>(message);
		}

		

		public void Delete<TMessage>(string queue) where TMessage : class
		{
			Bus.Delete<TMessage>(queue);
		}

		public void Delete<TMessage>() where TMessage : class
		{
			Bus.Delete<TMessage>();
		}

		public void Delete(string queue)
		{
			Bus.Delete(queue);
		}

		public void Publish<TMessage>(TMessage message, string topic, string exchangeName = null) where TMessage : class
		{
			Bus.Publish<TMessage>(message, topic, exchangeName);
		}

		public void Send<TMessage>(TMessage message) where TMessage : class
		{
			Send(string.Empty, message, false);
		}


		public List<TMessage> GetMessageExchange<TMessage>(string queueName, int count)
		{
			return Bus.GetMessageExchange<TMessage>(queueName, count);
		}

	
		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
			where TRequest : class
			where TResponse : class
		{
			return Bus.Respond<TRequest, TResponse>(response);
		}

		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class
		{
			return Bus.Respond<TRequest, TResponse>(response, queueName);
		}



		public void Dispose()
		{
			using (Bus)
			{
				
			}
		}


		public List<TMessage> GetMessageExchange<TMessage>(string queueName)
		{
			return Bus.GetMessageExchange<TMessage>(queueName);
		}
	}
}
