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
	public class SimpleBus : ISimpleBus
	{
		private readonly ISimpleConnection _simpleConnection;
		private readonly ILocalLogger _logger;
		private readonly ISerializeService _serializeService;
		private readonly InternalBus _internalBus;

		private int _treadCount = 0;
		private readonly object _lock = new object();
		public SimpleBus(ISimpleConnection simpleConnection, ILocalLogger logger, ISerializeService serializeService)
		{
			_simpleConnection = simpleConnection;
			_logger = logger;
			_serializeService = serializeService ?? new SerializeService();
			_internalBus = InternalBus.Create(_serializeService, logger);
		}
		public SimpleBus(ISimpleConnection simpleConnection, ILocalLogger logger)
			: this(simpleConnection, logger, null)
		{
		}

		public SimpleBus(ISimpleConnection simpleConnection)
		 : this(simpleConnection, null, null)
		{
		}

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
		private void QueueingBasicConsumer<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel, Action<Func<IModel>> configure)
		{
			//if (!TryGet(queueName, getChannel, configure, out e, ref _consumer))
			//	return null;
			if (getChannel().IsClosed || _consumer == null)
			{
				configure.Invoke(getChannel);
				_consumer = new EventingBasicConsumer(getChannel.Invoke());
				getChannel().BasicConsume(queueName, false, _consumer);

				ProcessFromLocalBus(queueName, action, getChannel);
				if (_tasks.Count < _messageHandlerSettings.DegreeOfParallelism)
					_tasks.Add(new AsyncWorker(_logger, _queue));
				_consumer.Received += consumerOnReceived(queueName, action, getChannel);
			}
		}

		private void ProcessFromLocalBus<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel)
		{
			try
			{

				var items = _internalBus.Get(queueName);
				if (items.FastAny())
				{
					foreach (var item in items)
					{
						Action a = () =>
						{
							Increment();
							try
							{
								ProcessMessage(queueName, action, getChannel, item, default(TMessage));
							}
							catch (Exception e)
							{
								_logger?.Error($"Error queueName={queueName}: {e.Message}", e);
							}
							finally
							{
								Decrement();
							}
						};

						_queue.Enqueue(a);
					}
				}
			}
			catch (Exception e)
			{
				_logger.Error(e.Message, e);
			}
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
				_logger.Debug($"Invoke task messageId:{item.Id}");
				action.Invoke(message);
				_internalBus.Ack(item.Id);
				_logger.Debug($"Ack messageId:{item.Id}");
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
						var header = headers[_retrycount];
						retryCount = int.Parse(header?.ToString());
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
		private List<AsyncWorker> _tasks = new List<AsyncWorker>(5);
		private ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
		private EventHandler<BasicDeliverEventArgs> consumerOnReceived<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel)
		{
			return (ch, e) =>
			{
				ulong? deliveryTag = null;
				try
				{
					//ProcessFromLocalBus(queueName, action, getChannel);
					var tasks = _tasks.Where(x => x.IsActive).ToList();
					while (tasks.Count >= _messageHandlerSettings.DegreeOfParallelism)
					{
						Thread.Sleep(0);
						_logger.Debug($"To many threads Sleep {_queueName};{tasks.Count}>{_messageHandlerSettings.DegreeOfParallelism}");
						tasks = _tasks.Where(x => x.IsActive).ToList();
					}
					var body = e.Body;
					var properties = e.BasicProperties;
					var headers = properties.Headers ?? new Dictionary<string, object>();
					var messageAsString = Encoding.UTF8.GetString(body.ToArray());
					try
					{
						TMessage message = _serializeService.Deserialize<TMessage>(messageAsString);

						var item = _internalBus.Register(e, queueName, messageAsString);
						getChannel().BasicAck(e.DeliveryTag, false);

						Action a = () =>
						{
							Increment();
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
						};

						_queue.Enqueue(a);
						if (_tasks.Count < _messageHandlerSettings.DegreeOfParallelism)
							_tasks.Add(new AsyncWorker(_logger, _queue));
					}
					catch (JsonException jre)
					{
						deliveryTag = e.DeliveryTag;
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
							if (headers.ContainsKey(_retrycount))
							{
								var header = headers[_retrycount];
								retryCount = int.Parse(header?.ToString());
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
				catch (System.TimeoutException te)
				{
					_logger?.Error(te.Message, te);
					_consumer.Received -= consumerOnReceived(queueName, action, getChannel);
					_consumer = null;
					throw;
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
					Task.Delay(500);
				}
				catch (Exception ex)
				{
					_logger?.Error(ex.Message, ex);
				}
				finally
				{
					if (deliveryTag.HasValue)
					{
						var channel = getChannel();
						channel.BasicReject(deliveryTag.Value, true);
					}
					// getChannel.Invoke().BasicAck(ea.DeliveryTag, false);
					// ... process the message
					//if (e != null)
					//	getChannel().BasicAck(e.DeliveryTag, false);

				}
			};
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
				Task.Delay(_sleepTime);
				//на прthrow;
			}

		}

		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class
		{
			throw new NotImplementedException();
			var queue = queueName ?? GetQueue<TRequest>();
			TResponse resp = null;
			Consume<TRequest>(queue,
				(request) =>
				{
					resp = response.Invoke(request);
					//DeliverMessage(resp, e);
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