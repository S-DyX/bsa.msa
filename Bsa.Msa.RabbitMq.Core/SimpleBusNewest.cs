using Bsa.Msa.Common;
using Bsa.Msa.Common.Helpers;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Common;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.RabbitMq.Core
{
	public class SimpleBusNewest : ISimpleBus
	{
		private readonly ISimpleConnection _simpleConnection;
		private readonly ILocalLogger _logger;
		private readonly ISerializeService _serializeService;
		private readonly InternalBus _internalBus;

		private int _treadCount = 0;
		private readonly object _lock = new object();

		private void Increment()
		{
			lock (_lock)
			{

				Interlocked.Increment(ref _treadCount);
			}
		}
		private void Decrement()
		{
			lock (_lock)
				Interlocked.Decrement(ref _treadCount);
		}
		public SimpleBusNewest(ISimpleConnection simpleConnection, ILocalLogger logger, ISerializeService serializeService)
		{
			_simpleConnection = simpleConnection;
			_logger = logger;
			_serializeService = serializeService ?? new SerializeService();
			_internalBus = InternalBus.Create(_serializeService, logger);
		}
		public SimpleBusNewest(ISimpleConnection simpleConnection, ILocalLogger logger)
			: this(simpleConnection, logger, null)
		{
		}

		public SimpleBusNewest(ISimpleConnection simpleConnection)
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

		private QueueingBasicConsumer _consumer;

		private void Consume<TMessage>(string queueName, Action<TMessage> action, Action<Func<IModel>> configure)
		{
			_queueName = queueName;
			// добавляем действия на подписку
			_simpleConnection.Add(getChannel =>
			{
				if (_messageHandlerSettings.ClearAfterStart)
					getChannel.Invoke().QueuePurge(queueName);
				var items = _internalBus.Get(queueName);
				if (items.FastAny())
				{
					Parallel.ForEach(items,
						new ParallelOptions() { MaxDegreeOfParallelism = _messageHandlerSettings.DegreeOfParallelism },
						item =>
						{
							try
							{

								ProcessMessage(queueName, action, getChannel, item, default(TMessage));
							}
							catch (Exception e)
							{
								_logger?.Error($"Error queueName={queueName}: {e.Message}", e);
							}

						});
				}
				while (!isTerminating)
				{
					var result = QueueingBasicConsumer(queueName, action, getChannel, configure);

				}
			});
			// событие соединение с RMQ
			_simpleConnection.BeforeConnect += () =>
			{
				_consumer = null;

			};
			_simpleConnection.AfterConnect += () =>
			{
				_logger?.Info($"AfterConnect End subscription: {_messageHandlerSettings.Type};");
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
			var array = body.ToArray();
			var message = Encoding.UTF8.GetString(array);
			try
			{
				data = _serializeService.Deserialize<TMessage>(message);
				isEmpty = false;
			}
			catch (Exception ex)
			{
				_logger?.Error(ex.Message, ex);
				SendErrorMessage(getChannel(), queueName, array, ex);
			}
			// ... process the message
			getChannel().BasicAck(getResult.DeliveryTag, false);
			return !isEmpty;
		}

		private const int _sleepTime = 200;
		private int _iteration = 1;
		private List<Task> _tasks = new List<Task>(5);
		private BasicDeliverEventArgs QueueingBasicConsumer<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel, Action<Func<IModel>> configure)
		{
			// если на шину будет больше одного подписчика то надо будет изменить логику
			byte[] messageArray = null;
			BasicDeliverEventArgs e = null;
			ulong? deliveryTag = null;
			IDictionary<string, object> headers = null;
			try
			{

				_tasks = _tasks.Where(x => x.Status == TaskStatus.Running).ToList();
				while (_treadCount >= _messageHandlerSettings.DegreeOfParallelism)
				{
					Thread.Sleep(0);
					_logger.Info($"Sleep");
				}

				if (!TryGet(queueName, getChannel, configure, out e, ref _consumer))
					return null;

				var body = e.Body;
				var properties = e.BasicProperties;
				headers = properties.Headers ?? new Dictionary<string, object>();
				messageArray = body.ToArray();
				var messageAsString = Encoding.UTF8.GetString(messageArray);
				try
				{
					TMessage message = _serializeService.Deserialize<TMessage>(messageAsString);
					var item = _internalBus.Register(e, queueName, messageAsString);
					getChannel().BasicAck(e.DeliveryTag, false);
					Increment();
					var task = Task.Factory.StartNew(() =>
					{
						try
						{
							ProcessMessage(queueName, action, getChannel, item, message);
						}
						catch (Exception e)
						{
							_logger?.Error($"Error queueName={queueName}: {e.Message}", e);
						}
						finally
						{
							Decrement();
						}
					});
					_tasks.Add(task);


				}
				catch (Exception ex)
				{
					if (ex is JsonException)
					{
						deliveryTag = e.DeliveryTag;
						e = null;
						_logger?.Error($"Error queueName={queueName}: {ex.Message};{System.Environment.NewLine}JSON:{messageAsString}", ex);
					}
					else
					{


						if (ex is AggregateException)
						{
							foreach (var innerException in ((AggregateException)ex).Flatten().InnerExceptions)
							{
								_logger?.Error($"Error queueName={queueName}: {innerException.Message};{System.Environment.NewLine}JSON:{messageAsString}",
									innerException);
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

							if (_messageHandlerSettings.RetryCount.HasValue &&
								_messageHandlerSettings.RetryCount.Value < retryCount)
							{
								_logger?.Error(
									$"retry count exceeded {retryCount}>{_messageHandlerSettings.RetryCount.Value}. Error queueName={queueName}: {ex.Message}",
									ex);
								SendErrorMessage(getChannel(), queueName, messageArray, ex);
							}
							else
							{
								_logger?.Error($"Error queueName={queueName}: {ex.Message}", ex);
								retryCount++;
								headers["retryCount"] = retryCount;
								Send(getChannel(), queueName, messageArray, headers);
							}

						}
						else
						{
							SendErrorMessage(getChannel(), queueName, messageArray, ex);
						}
					}

				}
			}
			catch (System.IO.EndOfStreamException endOfStreamException)
			{
				_logger?.Error(endOfStreamException.Message, endOfStreamException);
				_consumer = null;
				throw;
			}
			catch (OperationInterruptedException ex)
			{
				_logger?.Error($"{queueName};{ex.Message}", ex);
				Thread.Sleep(500);
			}
			catch (Exception ex)
			{
				_logger?.Error($"{queueName};{ex.Message}", ex);
				Thread.Sleep(200);
			}
			finally
			{
				// ... process the message
				//if (e != null)
				//	getChannel().BasicAck(e.DeliveryTag, false);
				if (deliveryTag.HasValue)
				{
					var channel = getChannel();
					channel.BasicReject(deliveryTag.Value, true);
				}

			}
			return e;
		}
		private static string _retrycount = "retryCount";
		private byte[] ProcessMessage<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel, InternalBusItem item, TMessage message)
		{
			byte[] messageArray = null;
			IDictionary<string, object> headers = item.Headers;
			try
			{
				if (message == null)
				{

					headers = item.Headers;
					message = _serializeService.Deserialize<TMessage>(item.Body);
				}
				_logger.Info($"Invoke task messageId:{item.Id}");
				action.Invoke(message);
				_internalBus.Ack(item.Id);
				_logger.Info($"Ack messageId:{item.Id}");
			}
			catch (Exception exception)
			{
				_logger?.Error($"messageId:{item.Id};Error queueName={queueName}: {exception.Message}", exception);
				messageArray = Encoding.UTF8.GetBytes(item.Body);
				if (_messageHandlerSettings.Retry)
				{
					var retryCount = 0;

					if (headers.ContainsKey(_retrycount))
					{
						retryCount = (int)headers[_retrycount];
					}

					if (_messageHandlerSettings.RetryCount.HasValue &&
						_messageHandlerSettings.RetryCount.Value < retryCount)
					{
						_logger?.Error(
							$"retry count exceeded {retryCount}>{_messageHandlerSettings.RetryCount.Value}. Error queueName={queueName}: {exception.Message}",
							exception);
						SendErrorMessage(getChannel(), queueName, messageArray, exception);
					}
					else
					{
						retryCount++;
						headers[_retrycount] = retryCount;
						Send(getChannel(), queueName, messageArray, headers);
					}
				}
				else
				{
					SendErrorMessage(getChannel(), queueName, messageArray, exception);
				}
				_internalBus.Ack(item.Id);
			}

			return messageArray;
		}

		private bool TryGet(string queueName, Func<IModel> getChannel, Action<Func<IModel>> configure, out BasicDeliverEventArgs e, ref QueueingBasicConsumer consumer, bool noAck = false, int interval = 20)
		{
			GetConsumer(queueName, getChannel, configure, ref consumer, noAck);
			//var data = GetMessageExchange<TMessage>(queueName);
			//var e = (BasicDeliverEventArgs) consumer.Queue.Dequeue();

			if (!consumer.Queue.Dequeue(interval, out e))
			{
				var millisecondsTimeout = _sleepTime * _iteration;
				Thread.Sleep(millisecondsTimeout);
				if (_iteration < 10)
					_iteration++;
				return false;

			}
			_iteration = 1;
			return e != null;
		}

		private static string GetConsumer(string queueName, Func<IModel> getChannel, Action<Func<IModel>> configure, ref QueueingBasicConsumer consumer, bool noAck)
		{
			var channel = getChannel();
			if (channel.IsClosed || consumer == null)
			{
				configure.Invoke(getChannel);

				consumer = new QueueingBasicConsumer(channel);
				return channel.BasicConsume(queueName, noAck, consumer);
			}

			return string.Empty;
		}

		private void SendErrorMessage(IModel channel, string queueName, byte[] body, Exception ex)
		{
			var errorQueue = queueName + ".Error";
			var dictionary = new Dictionary<string, object>(2)
			{
			};
			dictionary["error"] = ex.ToString();
			Send(channel, errorQueue, body, dictionary);
		}

		private bool isTerminating;
		public void Dispose()
		{
			isTerminating = true;
			using (_simpleConnection)
			{

			}

		}

		private void Send(IModel channel, string queue, byte[] body, IDictionary<string, object> headers)
		{
			try
			{

				var dictionary = GetArguments(_messageHandlerSettings);
				channel.QueueDeclare(queue, true, false, false, dictionary);
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
			});
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
		public TResponse Request<TMessage, TResponse>(TMessage message)
			where TMessage : class
			where TResponse : class
		{
			var exchangeName = SimpleBusExtension.GetExchangeName<TMessage>();
			_simpleConnection.Configure(exchangeName, getChannel =>
			{
				getChannel().ExchangeDeclare(exchangeName, fanout, true);
			});

			var corrId = Guid.NewGuid().ToString().Replace("-", "");
			var replyQueueName = message.GetType().Name + corrId;
			TResponse data = default(TResponse);
			var actionSend = new Action(() => SendRequest(message, exchangeName, corrId, replyQueueName));
			Parallel.Invoke(actionSend);

			_simpleConnection.Execute(getChannel =>
			{
				int iterationCount = 0;
				QueueingBasicConsumer consumer = null;
				var configure = GetRespondeConfigure(replyQueueName);
				BasicDeliverEventArgs e;
				while (!TryGet(replyQueueName, getChannel, configure, out e, ref consumer, noAck: true))
				{
					Thread.Sleep(_sleepTime);
					if (iterationCount > 10)
					{
						getChannel().BasicCancel(e.ConsumerTag);
						consumer = null;
						throw new TimeoutException($"Request time out. {replyQueueName}");
					}
					iterationCount++;
				}

				var body = e.Body;
				getChannel().BasicCancel(e.ConsumerTag);
				consumer = null;

				var messageAsString = Encoding.UTF8.GetString(body.ToArray());

				data = _serializeService.Deserialize<TResponse>(messageAsString);
			});



			return data;

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
				throw;
			}

		}

		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class
		{
			throw new NotImplementedException();

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
