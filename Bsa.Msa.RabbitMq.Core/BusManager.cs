using System;
using System.Collections.Generic;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;

namespace Bsa.Msa.RabbitMq.Core
{
	/// <inheritdoc />
	public sealed class BusManager : IBusManager
	{ 
		private readonly ISimpleBus _simpleBus;
		private readonly ILocalBus _localBus;
		private readonly object _sync = new object();

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="simpleBus"></param>
		/// <param name="localBus"></param>
		public BusManager(ISimpleBus simpleBus, ILocalBus localBus)
		{ 
			_simpleBus = simpleBus;
			_localBus = localBus;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="simpleBus"></param>
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
					_simpleBus.Reconnect("From bus");
				}
				return _simpleBus;
			}
		}

		public void SendSelf<TMessage>(TMessage message) where TMessage : class
		{
			Bus.SendSelf(message);
		}

		/// <inheritdoc />
		public void Send<TMessage>(string queue, TMessage message, int? ttl = null, bool forceSend = false) where TMessage : class
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
						Bus.Send<TMessage>(queue, message, ttl);
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
					Bus.Send<TMessage>(queue, message, ttl);
				}
			}
		}

		/// <inheritdoc />
		public void Publish<TMessage>(TMessage message) where TMessage : class
		{
			Bus.Publish<TMessage>(message);
		}



		/// <inheritdoc />
		public void Delete<TMessage>(string queue) where TMessage : class
		{
			Bus.Delete<TMessage>(queue);
		}

		public void Delete<TMessage>() where TMessage : class
		{
			Bus.Delete<TMessage>();
		}

		/// <inheritdoc />
		public void Delete(string queue)
		{
			Bus.Delete(queue);
		}

		/// <inheritdoc />
		public void Publish<TMessage>(TMessage message, string topic, string exchangeName = null) where TMessage : class
		{
			Bus.Publish<TMessage>(message, topic, exchangeName);
		}

		/// <inheritdoc />
		public void Send<TMessage>(TMessage message, int? ttl = null) where TMessage : class
		{
			Send(string.Empty, message, ttl,false);
		}

		/// <inheritdoc />
		public List<TMessage> GetMessageExchange<TMessage>(string queueName, int count)
		{
			return Bus.GetMessageExchange<TMessage>(queueName, count);
		}

		/// <inheritdoc />
		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
			where TRequest : class
			where TResponse : class
		{
			return Bus.Respond<TRequest, TResponse>(response);
		}

		/// <inheritdoc />
		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class
		{
			return Bus.Respond<TRequest, TResponse>(response, queueName);
		}


		/// <inheritdoc />
		public void Dispose()
		{
			using (Bus)
			{
				
			}
		}

		/// <inheritdoc />
		public List<TMessage> GetMessageExchange<TMessage>(string queueName)
		{
			return Bus.GetMessageExchange<TMessage>(queueName);
		}
	}
}
