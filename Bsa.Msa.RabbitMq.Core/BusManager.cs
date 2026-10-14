using System;
using System.Collections.Generic;
using Bsa.Msa.RabbitMq.Core.Interfaces;

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
			// Without a queue the message goes to the exchange of the type,
			// so every subscribed queue (useExchange) receives it.
			if (string.IsNullOrEmpty(queue))
			{
				Bus.Publish(message, string.Empty, null, ttl);
				return;
			}

			if (!forceSend && TryHandleLocally(queue, message))
				return;

			// Broker errors are not retried here: a blind retry duplicated messages.
			Bus.Send(queue, message, ttl);
		}

		private bool TryHandleLocally<TMessage>(string queue, TMessage message)
		{
			if (_localBus == null) return false;

			try
			{
				return _localBus.Handle(queue, message);
			}
			catch (Exception)
			{
				// The local handler failed: the message goes through the broker,
				// where the handler retry settings apply.
				return false;
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
		public void Purge(string queueName)
		{
			Bus.Purge(queueName);
		}


		/// <inheritdoc />
		public void Dispose()
		{
			// The field, not the Bus property: disposing must not reconnect.
			_simpleBus.Dispose();
		}

		/// <inheritdoc />
		public List<TMessage> GetMessageExchange<TMessage>(string queueName)
		{
			return Bus.GetMessageExchange<TMessage>(queueName);
		}
	}
}
