using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bsa.Msa.Common;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Bsa.Msa.RabbitMq.Core
{
	public class SimpleBus : ISimpleBus
	{
		private readonly ISimpleConnection _simpleConnection;
		private readonly ILocalLogger _logger;
		private readonly ISerializeService _serializeService;

		public SimpleBus(ISimpleConnection simpleConnection, ILocalLogger logger, ISerializeService serializeService)
		{
			_simpleConnection = simpleConnection;
			_logger = logger;
			_serializeService = serializeService ?? new SerializeService();
		}
		public SimpleBus(ISimpleConnection simpleConnection, ILocalLogger logger)
			: this(simpleConnection, logger, null)
		{
		}

		public SimpleBus(ISimpleConnection simpleConnection)
		 : this(simpleConnection, null, null)
		{
		}

		private IMessageHandlerSettings _messageHandlerSettings;
		private string _queueName;

		public void Subscribe<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
		{
			_messageHandlerSettings = messageHandlerSettings;
			Action<Func<IModel>> configureAction = getChannel =>
			{
				var dictionary = GetArguments(messageHandlerSettings);
				getChannel().QueueDeclare(queueName, true, false, false, dictionary);
				getChannel().BasicQos(0, messageHandlerSettings.PrefetchCount, false);
			};
			//_simpleConnection.Configure(queueName, configureAction);

			Consume<TMessage>(queueName, action, configureAction);

		}

		private static IDictionary<string, object> GetArguments(IMessageHandlerSettings messageHandlerSettings)
		{
			IDictionary<string, object> arguments = new Dictionary<string, object>(0);
			if (messageHandlerSettings.Ttl.HasValue)
			{
				arguments = new ConcurrentDictionary<string, object>();
				arguments["x-message-ttl"] = messageHandlerSettings.Ttl.Value;
			}
			return arguments;
		}


		public void SubscribeExchange<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
		{
			_messageHandlerSettings = messageHandlerSettings;
			//ConfigureExchange<TMessage>(queueName);

			Consume<TMessage>(queueName, action, GetExchangeConfigure<TMessage>(queueName, messageHandlerSettings));


		}

		public void SendSelf<TMessage>(TMessage message) where TMessage : class
		{
			if (string.IsNullOrEmpty(_queueName))
				throw new InvalidOperationException("Can not send message back");
			Send(_queueName, message);
		}

		//private QueueingBasicConsumer _consumer;
		private EventingBasicConsumer _consumer;


		private void Consume<TMessage>(string queueName, Action<TMessage> action, Action<Func<IModel>> configure)
		{
			Consume<TMessage>(queueName, (message, e) => action.Invoke(message), configure);
		}
		private void Consume<TMessage>(string queueName, Action<TMessage, BasicDeliverEventArgs> action, Action<Func<IModel>> configure)
		{
			_queueName = queueName;
			// добавляем действия на подписку
			_simpleConnection.Add(getChannel =>
			{
				//while (!isTerminating)
				{
					QueueingBasicConsumer(queueName, action, getChannel, configure);

				}
			});
			// событие соединение с RMQ
			_simpleConnection.BeforeConnect += () =>
			{
				_consumer = null;

			};
			_simpleConnection.AfterConnect += () =>
			{

			};
			// выполняем подписку
			_simpleConnection.SubscribeAll();
		}

		private void ConfigureExchange<TMessage>(string queueName, IMessageHandlerSettings settings)
		{
			var action = GetExchangeConfigure<TMessage>(queueName, settings);
			_simpleConnection.Configure(queueName, action);
		}

		private Action<Func<IModel>> GetExchangeConfigure<TMessage>(string queueName, IMessageHandlerSettings settings)
		{
			var routingKey = string.Empty;
			IDictionary<string, object> dictionary = null;
			if (settings != null)
			{
				routingKey = settings.RoutingKey;
				dictionary = GetArguments(settings);
			}
			var type = fanout;
			if (!string.IsNullOrEmpty(routingKey))
				type = topic;

			Action<Func<IModel>> action = getModel =>
			{
				var exchangeName = SimpleBusExtension.GetExchangeName<TMessage>();
				getModel().ExchangeDeclare(exchangeName, type, true);
				getModel().QueueDeclare(queueName, true, false, false, dictionary);
				getModel().QueueBind(queueName, exchangeName, routingKey ?? String.Empty);
				getModel().BasicQos(0, _messageHandlerSettings?.PrefetchCount ?? (ushort)5, false);

			};
			return action;
		}
		private Action<Func<IModel>> GetRespondeConfigure(string queueName)
		{
			Action<Func<IModel>> action = getModel =>
			{
				getModel().QueueDeclare(queueName, false, false, true, null);
				getModel().BasicQos(0, _messageHandlerSettings != null ? _messageHandlerSettings.PrefetchCount : (ushort)5, false);
			};
			return action;
		}

		public void Delete<TMessage>() where TMessage : class
		{
			var exchangeName = SimpleBusExtension.GetExchangeName<TMessage>();
			_simpleConnection.Configure(exchangeName, getChannel =>
			{
				getChannel().ExchangeDelete(exchangeName, true);
			});
		}

		public List<TMessage> GetMessageExchange<TMessage>(string queueName)
		{
			//int take = 20;

			//var result = new ConcurrentBag<TMessage>();
			//ConfigureExchange<TMessage>(queueName, null);

			////_simpleConnection.
			//_simpleConnection.Execute(getChannel =>
			//{
			//	for (int i = 0; i < take; i++)
			//	{
			//		TMessage data;
			//		if (TryGetMessage(queueName, getChannel, out data))
			//			result.Add(data);
			//	}

			//});
			//return result.ToList();
			return GetMessageExchange<TMessage>(queueName, 20);
		}

		public List<TMessage> GetMessageExchange<TMessage>(string queueName, int count)
		{
			//int take = 20;

			var result = new ConcurrentBag<TMessage>();
			ConfigureExchange<TMessage>(queueName, null);

			//_simpleConnection.
			_simpleConnection.Execute(getChannel =>
			{
				for (int i = 0; i < count; i++)
				{
					TMessage data;
					if (TryGetMessage(queueName, getChannel, out data))
						result.Add(data);
				}

			});
			return result.ToList();
		}

		private bool TryGetMessage<TMessage>(string queueName, Func<IModel> getChannel, out TMessage data)
		{
			data = default(TMessage);
			var isEmpty = true;
			var getResult = getChannel().BasicGet(queueName, false);
			if (getResult == null)
				return false;
			var body = getResult.Body;
			var message = Encoding.UTF8.GetString(body.ToArray());
			try
			{
				data = _serializeService.Deserialize<TMessage>(message);
				isEmpty = false;
			}
			catch (Exception ex)
			{
				_logger?.Error(ex.Message, ex);
				SendErrorMessage(getChannel(), queueName, message, ex);
			}

			// ... process the message
			getChannel().BasicAck(getResult.DeliveryTag, false);
			return !isEmpty;
		}

		private const int _sleepTime = 500;
		private int _iteration = 1;
		private void QueueingBasicConsumer<TMessage>(string queueName, Action<TMessage, BasicDeliverEventArgs> action, Func<IModel> getChannel, Action<Func<IModel>> configure)
		{
			//if (!TryGet(queueName, getChannel, configure, out e, ref _consumer))
			//	return null;
			if (getChannel().IsClosed || _consumer == null)
			{
				configure.Invoke(getChannel);
				_consumer = new EventingBasicConsumer(getChannel.Invoke());
				getChannel().BasicConsume(queueName, false, _consumer);
				_consumer.Received += consumerOnReceived(queueName, action, getChannel);
			}
		}

		private EventHandler<BasicDeliverEventArgs> consumerOnReceived<TMessage>(string queueName, Action<TMessage, BasicDeliverEventArgs> action, Func<IModel> getChannel)
		{
			return (ch, e) =>
			{
				try
				{

					var body = e.Body;
					var properties = e.BasicProperties;
					var headers = properties.Headers ?? new Dictionary<string, object>();
					var messageAsString = Encoding.UTF8.GetString(body.ToArray());
					try
					{
						TMessage message = _serializeService.Deserialize<TMessage>(messageAsString);
						action.Invoke(message, e);
					}
					catch (JsonException jre)
					{
						_logger?.Error($"{messageAsString};{jre.Message};{System.Environment.NewLine}JSON:{messageAsString}", jre);
					}
					catch (Exception ex)
					{
						if (ex is AggregateException)
						{
							foreach (var innerException in ((AggregateException)ex).Flatten().InnerExceptions)
							{
								_logger?.Error($"Error queueName={queueName}: {innerException.Message};{System.Environment.NewLine}JSON:{messageAsString}", innerException);
							}
						}
						else
						{
							_logger?.Error($"Error queueName={queueName}: {ex.Message};{System.Environment.NewLine}JSON:{messageAsString}", ex);
						}
						if (_messageHandlerSettings.Retry)
						{
							var retryCount = 0;
							if (headers.ContainsKey("retryCount"))
							{
								retryCount = (int)headers["retryCount"];
							}
							if (_messageHandlerSettings.RetryCount.HasValue && _messageHandlerSettings.RetryCount.Value < retryCount)
							{
								_logger?.Error(
									$"retry count exceeded {retryCount}>{_messageHandlerSettings.RetryCount.Value}. Error queueName={queueName}: {ex.Message}",
									ex);
								SendErrorMessage(getChannel(), queueName, messageAsString, ex);
							}
							else
							{
								retryCount++;
								headers["retryCount"] = retryCount;
								Send(getChannel(), queueName, messageAsString, headers);
							}

						}
						else
						{
							SendErrorMessage(getChannel(), queueName, messageAsString, ex);
						}

					}
				}
				catch (System.IO.EndOfStreamException endOfStreamException)
				{
					_logger?.Error(endOfStreamException.Message, endOfStreamException);
					_consumer.Received -= consumerOnReceived(queueName, action, getChannel);
					_consumer = null;
					throw;
				}
				catch (OperationInterruptedException ex)
				{
					_logger?.Error(ex.Message, ex);
					Thread.Sleep(500);
				}
				catch (Exception ex)
				{
					_logger?.Error(ex.Message, ex);
				}
				finally
				{
					// getChannel.Invoke().BasicAck(ea.DeliveryTag, false);
					// ... process the message
					if (e != null)
						getChannel().BasicAck(e.DeliveryTag, false);

				}
			};
		}


		private void SendErrorMessage(IModel channel, string queueName, string message, Exception ex)
		{
			var errorQueue = queueName + ".Error";
			var errorMessage = new
			{
				Error = ex.ToString(),
				Body = message,
			};
			var em = _serializeService.Serialize(errorMessage);
			Send(channel, errorQueue, em, new Dictionary<string, object>());
		}

		private bool isTerminating;
		public void Dispose()
		{
			isTerminating = true;


			using (_simpleConnection)
			{

			}
			if (_consumer != null)
			{

				//_consumer.Received += consumerOnReceived;
			}
		}

		private void Send(IModel channel, string queue, string message, IDictionary<string, object> headers)
		{
			try
			{

				var dictionary = GetArguments(_messageHandlerSettings);
				channel.QueueDeclare(queue, true, false, false, dictionary);
				var body = Encoding.UTF8.GetBytes(message);
				var props = channel.CreateBasicProperties();
				props.DeliveryMode = 2;
				props.Headers = headers;
				channel.BasicPublish("", queue, props, body);
			}
			catch (Exception ex)
			{
				_logger?.Error(ex.Message, ex);
			}
		}






		public void Send<TMessage>(TMessage message) where TMessage : class
		{
			var queueName = SimpleBusExtension.GetQueueName<TMessage>();
			Send(queueName, message);

		}

		public void Send<TMessage>(string queue, TMessage message) where TMessage : class
		{
			_simpleConnection.Configure(queue, getChannel =>
			{
				getChannel().QueueDeclare(queue, true, false, false, null);
			}, true);
			_simpleConnection.Execute(getChannel =>
			{
				var m = _serializeService.Serialize(message);
				var body = Encoding.UTF8.GetBytes(m);
				var props = getChannel().CreateBasicProperties();
				props.DeliveryMode = 2;
				getChannel().BasicPublish("", queue, props, body);
			});

		}

		public void Publish<TMessage>(TMessage message) where TMessage : class
		{
			Publish(message, String.Empty);
		}

		private static string fanout = "fanout";
		private static string topic = "topic";

		public void Publish<TMessage>(TMessage message, string routingKey, string exchangeName = null) where TMessage : class
		{
			var type = fanout;
			if (!string.IsNullOrEmpty(routingKey))
				type = topic;

			exchangeName = exchangeName ?? SimpleBusExtension.GetExchangeName<TMessage>();
			_simpleConnection.Configure(exchangeName, getChannel =>
			{

				getChannel().ExchangeDeclare(exchangeName, type, true);
			});

			_simpleConnection.Execute(getChannel =>
			{

				var m = _serializeService.Serialize(message);
				var body = Encoding.UTF8.GetBytes(m);
				var props = getChannel().CreateBasicProperties();
				props.DeliveryMode = 2;
				getChannel().BasicPublish(exchangeName, routingKey ?? String.Empty, props, body);
			});
		}

		public void Delete<TMessage>(string queue) where TMessage : class
		{
			var exchangeName = SimpleBusExtension.GetExchangeName<TMessage>();
			_simpleConnection.Configure(exchangeName, getChannel =>
			{
				getChannel().ExchangeDelete(exchangeName, true);
				getChannel().QueueDelete(queue);
			});
		}
		public void Delete(string queue)
		{
			_simpleConnection.Execute(getChannel =>
			{
				getChannel().QueueDelete(queue);
			});
		}


		private void SendRequest<TMessage>(TMessage message, string exchangeName, string corrId, string replyQueueName) where TMessage : class
		{
			_simpleConnection.Execute(getChannel =>
			{
				var m = _serializeService.Serialize(message);
				var body = Encoding.UTF8.GetBytes(m);

				var props = getChannel().CreateBasicProperties();
				props.ReplyTo = replyQueueName;
				props.CorrelationId = corrId;
				getChannel().BasicPublish(exchangeName, string.Empty, props, body);
			});
		}

		public bool IsConnected
		{
			get { return _simpleConnection.IsConnected; }
		}


		public void Reconnect()
		{
			try
			{
				_simpleConnection.Reconnect();

			}
			catch
			{
				Thread.Sleep(_sleepTime);
				//на прthrow;
			}

		}

		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class
		{
			var queue = queueName ?? GetQueue<TRequest>();
			TResponse resp = null;
			Consume<TRequest>(queue,
				(request, e) =>
				{
					resp = response.Invoke(request);
					DeliverMessage(resp, e);
				},
				GetExchangeConfigure<TRequest>(queue, null)
			);

			return null;
		}
		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
			where TRequest : class
			where TResponse : class
		{
			return Respond<TRequest, TResponse>(response, null);
		}

		private string GetQueue<TRequest>() where TRequest : class
		{
			return _messageHandlerSettings != null ? _messageHandlerSettings.SubscriptionEndpoint ?? SimpleBusExtension.GetQueueName<TRequest>() : SimpleBusExtension.GetQueueName<TRequest>();
		}

		private void DeliverMessage<TResponse>(TResponse resp, BasicDeliverEventArgs ea) where TResponse : class
		{
			var props = ea.BasicProperties;
			_simpleConnection.Execute(getChannel =>
			{
				var replyProps = getChannel().CreateBasicProperties();
				replyProps.CorrelationId = props.CorrelationId;
				var res = _serializeService.Serialize(resp);
				var b = Encoding.UTF8.GetBytes(res);
				getChannel().BasicPublish("", props.ReplyTo, replyProps, b);
				//getChannel().BasicAck(ea.DeliveryTag, false);
			});
		}
	}
}
//using RabbitMQ.Client;
//using RabbitMQ.Client.Events;
//using RabbitMQ.Client.Exceptions;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.Json;
//using System.Threading;
//using Bsa.Msa.RabbitMq.Core.Common;
//using Bsa.Msa.RabbitMq.Core.Common.Services.MessageHandling;
//using Bsa.Msa.RabbitMq.Core.Interfaces;

//namespace Bsa.Msa.RabbitMq.Core
//{
//	public class SimpleBus : ISimpleBus
//	{
//		private readonly ISimpleConnection _simpleConnection;
//		private readonly ILocalLogger _logger;
//		private readonly ISerializeService _serializeService;

//		public SimpleBus(ISimpleConnection simpleConnection, ILocalLogger logger, ISerializeService serializeService)
//		{
//			_simpleConnection = simpleConnection;
//			_logger = logger;
//			_serializeService = serializeService ?? new SerializeService();
//		}
//		public SimpleBus(ISimpleConnection simpleConnection, ILocalLogger logger)
//			: this(simpleConnection, logger, null)
//		{
//		}

//		public SimpleBus(ISimpleConnection simpleConnection)
//		 : this(simpleConnection, null, null)
//		{
//		}

//		private IMessageHandlerSettings _messageHandlerSettings;
//		private string _queueName;

//		public void Subscribe<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
//		{
//			_messageHandlerSettings = messageHandlerSettings;
//			Action<Func<IModel>> configureAction = getChannel =>
//			{
//				var dictionary = GetArguments(messageHandlerSettings);
//				getChannel().QueueDeclare(queueName, true, false, false, dictionary);
//				getChannel().BasicQos(0, messageHandlerSettings.PrefetchCount, false);
//			};
//			//_simpleConnection.Configure(queueName, configureAction);

//			Consume<TMessage>(queueName, action, configureAction);

//		}

//		private static IDictionary<string, object> GetArguments(IMessageHandlerSettings messageHandlerSettings)
//		{
//			IDictionary<string, object> arguments = new Dictionary<string, object>(0);
//			if (messageHandlerSettings.Ttl.HasValue)
//			{
//				arguments = new ConcurrentDictionary<string, object>();
//				arguments["x-message-ttl"] = messageHandlerSettings.Ttl.Value;
//			}
//			return arguments;
//		}


//		public void SubscribeExchange<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
//		{
//			_messageHandlerSettings = messageHandlerSettings;
//			//ConfigureExchange<TMessage>(queueName);

//			Consume<TMessage>(queueName, action, GetExchangeConfigure<TMessage>(queueName, messageHandlerSettings));


//		}

//		public void SendSelf<TMessage>(TMessage message) where TMessage : class
//		{
//			if (string.IsNullOrEmpty(_queueName))
//				throw new InvalidOperationException("Can not send message back");
//			Send(_queueName, message);
//		}

//		//private QueueingBasicConsumer _consumer;
//		private EventingBasicConsumer _consumer;


//		private void Consume<TMessage>(string queueName, Action<TMessage> action, Action<Func<IModel>> configure)
//		{
//			Consume<TMessage>(queueName, (message, e) => action.Invoke(message), configure);
//		}
//		private void Consume<TMessage>(string queueName, Action<TMessage, BasicDeliverEventArgs> action, Action<Func<IModel>> configure)
//		{
//			_queueName = queueName;
//			// добавляем действия на подписку
//			_simpleConnection.Add(getChannel =>
//			{
//				//while (!isTerminating)
//				{
//					QueueingBasicConsumer(queueName, action, getChannel, configure);

//				}
//			});
//			// событие соединение с RMQ
//			_simpleConnection.BeforeConnect += () =>
//			 {
//				 _consumer = null;

//			 };
//			_simpleConnection.AfterConnect += () =>
//			{

//			};
//			// выполняем подписку
//			_simpleConnection.SubscribeAll();
//		}

//		private void ConfigureExchange<TMessage>(string queueName, IMessageHandlerSettings settings)
//		{
//			var action = GetExchangeConfigure<TMessage>(queueName, settings);
//			_simpleConnection.Configure(queueName, action);
//		}

//		private Action<Func<IModel>> GetExchangeConfigure<TMessage>(string queueName, IMessageHandlerSettings settings)
//		{
//			var routingKey = string.Empty;
//			IDictionary<string, object> dictionary = null;
//			if (settings != null)
//			{
//				routingKey = settings.RoutingKey;
//				dictionary = GetArguments(settings);
//			}
//			var type = fanout;
//			if (!string.IsNullOrEmpty(routingKey))
//				type = topic;

//			Action<Func<IModel>> action = getModel =>
//			{
//				var exchangeName = SimpleBusExtension.GetExchangeName<TMessage>();
//				getModel().ExchangeDeclare(exchangeName, type, true);
//				getModel().QueueDeclare(queueName, true, false, false, dictionary);
//				getModel().QueueBind(queueName, exchangeName, routingKey ?? String.Empty);
//				getModel().BasicQos(0, _messageHandlerSettings?.PrefetchCount ?? (ushort)5, false);

//			};
//			return action;
//		}
//		private Action<Func<IModel>> GetRespondeConfigure(string queueName)
//		{
//			Action<Func<IModel>> action = getModel =>
//			{
//				getModel().QueueDeclare(queueName, false, false, true, null);
//				getModel().BasicQos(0, _messageHandlerSettings != null ? _messageHandlerSettings.PrefetchCount : (ushort)5, false);
//			};
//			return action;
//		}

//		public void Delete<TMessage>() where TMessage : class
//		{
//			var exchangeName = SimpleBusExtension.GetExchangeName<TMessage>();
//			_simpleConnection.Configure(exchangeName, getChannel =>
//			{
//				getChannel().ExchangeDelete(exchangeName, true);
//			});
//		}

//		public List<TMessage> GetMessageExchange<TMessage>(string queueName)
//		{
//			//int take = 20;

//			//var result = new ConcurrentBag<TMessage>();
//			//ConfigureExchange<TMessage>(queueName, null);

//			////_simpleConnection.
//			//_simpleConnection.Execute(getChannel =>
//			//{
//			//	for (int i = 0; i < take; i++)
//			//	{
//			//		TMessage data;
//			//		if (TryGetMessage(queueName, getChannel, out data))
//			//			result.Add(data);
//			//	}

//			//});
//			//return result.ToList();
//			return GetMessageExchange<TMessage>(queueName, 20);
//		}

//		public List<TMessage> GetMessageExchange<TMessage>(string queueName, int count)
//		{
//			//int take = 20;

//			var result = new ConcurrentBag<TMessage>();
//			ConfigureExchange<TMessage>(queueName, null);

//			//_simpleConnection.
//			_simpleConnection.Execute(getChannel =>
//			{
//				for (int i = 0; i < count; i++)
//				{
//					TMessage data;
//					if (TryGetMessage(queueName, getChannel, out data))
//						result.Add(data);
//				}

//			});
//			return result.ToList();
//		}

//		private bool TryGetMessage<TMessage>(string queueName, Func<IModel> getChannel, out TMessage data)
//		{
//			data = default(TMessage);
//			var isEmpty = true;
//			var getResult = getChannel().BasicGet(queueName, false);
//			if (getResult == null)
//				return false;
//			var body = getResult.Body;
//			var message = Encoding.UTF8.GetString(body.ToArray());
//			try
//			{
//				data = _serializeService.Deserialize<TMessage>(message);
//				isEmpty = false;
//			}
//			catch (Exception ex)
//			{
//				_logger?.Error(ex.Message, ex);
//				SendErrorMessage(getChannel(), queueName, message, ex);
//			}

//			// ... process the message
//			getChannel().BasicAck(getResult.DeliveryTag, false);
//			return !isEmpty;
//		}

//		private const int _sleepTime = 500;
//		private int _iteration = 1;
//		private void QueueingBasicConsumer<TMessage>(string queueName, Action<TMessage, BasicDeliverEventArgs> action, Func<IModel> getChannel, Action<Func<IModel>> configure)
//		{
//			//if (!TryGet(queueName, getChannel, configure, out e, ref _consumer))
//			//	return null;
//			if (getChannel().IsClosed || _consumer == null)
//			{
//				configure.Invoke(getChannel);
//				_consumer = new EventingBasicConsumer(getChannel.Invoke());
//				getChannel().BasicConsume(queueName, false, _consumer);
//				_consumer.Received += consumerOnReceived(queueName, action, getChannel);
//			}
//		}

//		private EventHandler<BasicDeliverEventArgs> consumerOnReceived<TMessage>(string queueName, Action<TMessage, BasicDeliverEventArgs> action, Func<IModel> getChannel)
//		{
//			return (ch, e) =>
//			{
//				try
//				{

//					var body = e.Body;
//					var properties = e.BasicProperties;
//					var headers = properties.Headers ?? new Dictionary<string, object>();
//					var messageAsString = Encoding.UTF8.GetString(body.ToArray());
//					try
//					{
//						TMessage message = _serializeService.Deserialize<TMessage>(messageAsString);
//						action.Invoke(message, e);
//					}
//					catch (JsonException jre)
//					{
//						_logger?.Error($"{messageAsString};{jre.Message}", jre);
//					}
//					catch (Exception ex)
//					{
//						if (ex is AggregateException)
//						{
//							foreach (var innerException in ((AggregateException)ex).Flatten().InnerExceptions)
//							{
//								_logger?.Error($"Error queueName={queueName}: {innerException.Message}", innerException);
//							}
//						}
//						else
//						{
//							_logger?.Error($"Error queueName={queueName}: {ex.Message}", ex);
//						}
//						if (_messageHandlerSettings.Retry)
//						{
//							var retryCount = 0;
//							if (headers.ContainsKey("retryCount"))
//							{
//								retryCount = (int)headers["retryCount"];
//							}
//							if (_messageHandlerSettings.RetryCount.HasValue && _messageHandlerSettings.RetryCount.Value < retryCount)
//							{
//								_logger?.Error(
//									$"retry count exceeded {retryCount}>{_messageHandlerSettings.RetryCount.Value}. Error queueName={queueName}: {ex.Message}",
//									ex);
//								SendErrorMessage(getChannel(), queueName, messageAsString, ex);
//							}
//							else
//							{
//								retryCount++;
//								headers["retryCount"] = retryCount;
//								Send(getChannel(), queueName, messageAsString, headers);
//							}

//						}
//						else
//						{
//							SendErrorMessage(getChannel(), queueName, messageAsString, ex);
//						}

//					}
//				}
//				catch (System.IO.EndOfStreamException endOfStreamException)
//				{
//					_logger?.Error(endOfStreamException.Message, endOfStreamException);
//					_consumer.Received -= consumerOnReceived(queueName, action, getChannel);
//					_consumer = null;
//					throw;
//				}
//				catch (OperationInterruptedException ex)
//				{
//					_logger?.Error(ex.Message, ex);
//					Thread.Sleep(500);
//				}
//				catch (Exception ex)
//				{
//					_logger?.Error(ex.Message, ex);
//				}
//				finally
//				{
//					// getChannel.Invoke().BasicAck(ea.DeliveryTag, false);
//					// ... process the message
//					if (e != null)
//						getChannel().BasicAck(e.DeliveryTag, false);

//				}
//			};
//		}

//		private bool TryGet(string queueName, Func<IModel> getChannel, Action<Func<IModel>> configure, out BasicDeliverEventArgs e, ref QueueingBasicConsumer consumer, bool noAck = false, int interval = 20)
//		{
//			GetConsumer(queueName, getChannel, configure, ref consumer, noAck);
//			//var data = GetMessageExchange<TMessage>(queueName);
//			//var e = (BasicDeliverEventArgs) consumer.Queue.Dequeue();

//			if (!consumer.Queue.Dequeue(interval, out e))
//			{
//				var millisecondsTimeout = _sleepTime * _iteration;
//				Thread.Sleep(millisecondsTimeout);
//				if (_iteration < 10)
//					_iteration++;
//				return false;

//			}
//			_iteration = 1;
//			return e != null;
//		}

//		private static string GetConsumer(string queueName, Func<IModel> getChannel, Action<Func<IModel>> configure, ref QueueingBasicConsumer consumer, bool noAck)
//		{
//			if (getChannel().IsClosed || consumer == null)
//			{
//				configure.Invoke(getChannel);

//				consumer = new QueueingBasicConsumer();
//				return getChannel().BasicConsume(queueName, noAck, consumer);
//			}

//			return string.Empty;
//		}

//		private void SendErrorMessage(IModel channel, string queueName, string message, Exception ex)
//		{
//			var errorQueue = queueName + ".Error";
//			var errorMessage = new
//			{
//				Error = ex.ToString(),
//				Body = message,
//			};
//			var em = _serializeService.Serialize(errorMessage);
//			Send(channel, errorQueue, em, new Dictionary<string, object>());
//		}

//		private bool isTerminating;
//		public void Dispose()
//		{
//			isTerminating = true;


//			using (_simpleConnection)
//			{

//			}
//			if (_consumer != null)
//			{

//				//_consumer.Received += consumerOnReceived;
//			}
//		}

//		private void Send(IModel channel, string queue, string message, IDictionary<string, object> headers)
//		{
//			try
//			{

//				var dictionary = GetArguments(_messageHandlerSettings);
//				channel.QueueDeclare(queue, true, false, false, dictionary);
//				var body = Encoding.UTF8.GetBytes(message);
//				var props = channel.CreateBasicProperties();
//				props.DeliveryMode = 2;
//				props.Headers = headers;
//				channel.BasicPublish("", queue, props, body);
//			}
//			catch (Exception ex)
//			{
//				_logger?.Error(ex.Message, ex);
//			}
//		}






//		public void Send<TMessage>(TMessage message) where TMessage : class
//		{
//			var queueName = SimpleBusExtension.GetQueueName<TMessage>();
//			Send(queueName, message);

//		}

//		public void Send<TMessage>(string queue, TMessage message) where TMessage : class
//		{
//			_simpleConnection.Configure(queue, getChannel =>
//			{
//				getChannel().QueueDeclare(queue, true, false, false, null);
//			}, true);
//			_simpleConnection.Execute(getChannel =>
//			{
//				var m = _serializeService.Serialize(message);
//				var body = Encoding.UTF8.GetBytes(m);
//				var props = getChannel().CreateBasicProperties();
//				props.DeliveryMode = 2;
//				getChannel().BasicPublish("", queue, props, body);
//			});

//		}

//		public void Publish<TMessage>(TMessage message) where TMessage : class
//		{
//			Publish(message, String.Empty);
//		}

//		private static string fanout = "fanout";
//		private static string topic = "topic";

//		public void Publish<TMessage>(TMessage message, string routingKey, string exchangeName = null) where TMessage : class
//		{
//			var type = fanout;
//			if (!string.IsNullOrEmpty(routingKey))
//				type = topic;

//			exchangeName = exchangeName ?? SimpleBusExtension.GetExchangeName<TMessage>();
//			_simpleConnection.Configure(exchangeName, getChannel =>
//			{

//				getChannel().ExchangeDeclare(exchangeName, type, true);
//			});

//			_simpleConnection.Execute(getChannel =>
//			{

//				var m = _serializeService.Serialize(message);
//				var body = Encoding.UTF8.GetBytes(m);
//				var props = getChannel().CreateBasicProperties();
//				props.DeliveryMode = 2;
//				getChannel().BasicPublish(exchangeName, routingKey ?? String.Empty, props, body);
//			});
//		}

//		public void Delete<TMessage>(string queue) where TMessage : class
//		{
//			var exchangeName = SimpleBusExtension.GetExchangeName<TMessage>();
//			_simpleConnection.Configure(exchangeName, getChannel =>
//			{
//				getChannel().ExchangeDelete(exchangeName, true);
//				getChannel().QueueDelete(queue);
//			});
//		}
//		public void Delete(string queue)
//		{
//			_simpleConnection.Execute(getChannel =>
//			{
//				getChannel().QueueDelete(queue);
//			});
//		}


//		private void SendRequest<TMessage>(TMessage message, string exchangeName, string corrId, string replyQueueName) where TMessage : class
//		{
//			_simpleConnection.Execute(getChannel =>
//			{
//				var m = _serializeService.Serialize(message);
//				var body = Encoding.UTF8.GetBytes(m);

//				var props = getChannel().CreateBasicProperties();
//				props.ReplyTo = replyQueueName;
//				props.CorrelationId = corrId;
//				getChannel().BasicPublish(exchangeName, string.Empty, props, body);
//			});
//		}

//		public bool IsConnected
//		{
//			get { return _simpleConnection.IsConnected; }
//		}


//		public void Reconnect()
//		{
//			try
//			{
//				_simpleConnection.Reconnect();

//			}
//			catch
//			{
//				Thread.Sleep(_sleepTime);
//				//на прthrow;
//			}

//		}

//		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
//			where TRequest : class
//			where TResponse : class
//		{
//			var queue = queueName ?? GetQueue<TRequest>();
//			TResponse resp = null;
//			Consume<TRequest>(queue,
//				(request, e) =>
//				{
//					resp = response.Invoke(request);
//					DeliverMessage(resp, e);
//				},
//				GetExchangeConfigure<TRequest>(queue, null)
//			);

//			return null;
//		}
//		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
//			where TRequest : class
//			where TResponse : class
//		{
//			return Respond<TRequest, TResponse>(response, null);
//		}

//		private string GetQueue<TRequest>() where TRequest : class
//		{
//			return _messageHandlerSettings != null ? _messageHandlerSettings.SubscriptionEndpoint ?? SimpleBusExtension.GetQueueName<TRequest>() : SimpleBusExtension.GetQueueName<TRequest>();
//		}

//		private void DeliverMessage<TResponse>(TResponse resp, BasicDeliverEventArgs ea) where TResponse : class
//		{
//			var props = ea.BasicProperties;
//			_simpleConnection.Execute(getChannel =>
//			{
//				var replyProps = getChannel().CreateBasicProperties();
//				replyProps.CorrelationId = props.CorrelationId;
//				var res = _serializeService.Serialize(resp);
//				var b = Encoding.UTF8.GetBytes(res);
//				getChannel().BasicPublish("", props.ReplyTo, replyProps, b);
//				//getChannel().BasicAck(ea.DeliveryTag, false);
//			});
//		}
//	}
//}