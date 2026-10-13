using Bsa.Msa.Common;
using Bsa.Msa.Common.Helpers;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Common;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.RabbitMq.Core
{
	/// <inheritdoc />
	public sealed class SimpleBus : ISimpleBus
	{
		private readonly ISimpleConnection _simpleConnection;
		private readonly ILocalLogger _logger;
		private readonly ISimpleBusNaming _busNaming;
		private readonly ISerializeService _serializeService;
		private readonly InternalBus _internalBus;

		private int _treadCount;
		private readonly object _lock = new object();

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="simpleConnection"></param>
		/// <param name="logger"></param>
		/// <param name="serializeService"></param>
		/// <param name="busNaming"></param>
		public SimpleBus(ISimpleConnection simpleConnection, ILocalLogger logger, ISerializeService serializeService, ISimpleBusNaming busNaming)
		{
			_simpleConnection = simpleConnection;
			_logger = logger;
			_busNaming = busNaming ?? new DefaultSimpleBusNaming();
			_serializeService = serializeService ?? new SerializeService();
			_internalBus = InternalBus.Create(_serializeService, logger);
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="simpleConnection"></param>
		/// <param name="logger"></param>
		/// <param name="busNaming"></param>
		public SimpleBus(ISimpleConnection simpleConnection, ILocalLogger logger, ISimpleBusNaming busNaming)
			: this(simpleConnection, logger, null, busNaming)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="simpleConnection"></param>
		/// <param name="busNaming"></param>
		public SimpleBus(ISimpleConnection simpleConnection, ISimpleBusNaming busNaming)
			: this(simpleConnection, null, null, busNaming)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="simpleConnection"></param>
		public SimpleBus(ISimpleConnection simpleConnection)
		 : this(simpleConnection, null, null, null)
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

		/// <inheritdoc />
		public void Subscribe<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
		{
			_messageHandlerSettings = messageHandlerSettings;
			if (messageHandlerSettings.AppendGuid)
			{
				queueName = $"{queueName}_{messageHandlerSettings.Uid}";
			}

			Action<Func<IModel>> configureAction = getChannel =>
			{
				var dictionary = GetArguments(messageHandlerSettings.Ttl);
				getChannel().QueueDeclare(queueName, true, false, messageHandlerSettings.AutoDelete, dictionary);
				getChannel().BasicQos(0, messageHandlerSettings.PrefetchCount, false);
			};
			//_simpleConnection.Configure(queueName, configureAction);

			Consume<TMessage>(queueName, action, configureAction);

		}

		/// <inheritdoc />
		public Action Shutdown { get; set; }

		private static IDictionary<string, object> GetArguments(int? ttl)
		{
			IDictionary<string, object> arguments = new Dictionary<string, object>(0);
			if (ttl.HasValue)
			{
				arguments = new ConcurrentDictionary<string, object>();
				arguments["x-message-ttl"] = ttl.Value;
			}

			return arguments;
		}

		/// <inheritdoc />
		public void SubscribeExchange<TMessage>(string queueName, Action<TMessage> action, IMessageHandlerSettings messageHandlerSettings)
		{
			_messageHandlerSettings = messageHandlerSettings;
			if (messageHandlerSettings.AppendGuid)
				queueName = $"{queueName}_{Guid.NewGuid()}";


			Consume(queueName, action, GetExchangeConfigure<TMessage>(queueName, messageHandlerSettings));


		}

		/// <inheritdoc />
		public void SendSelf<TMessage>(TMessage message) where TMessage : class
		{
			if (string.IsNullOrEmpty(_queueName))
				throw new InvalidOperationException("Can not send message back");
			Send(_queueName, message);
		}

		//private QueueingBasicConsumer _consumer;
		private EventingBasicConsumer _consumer;


		private bool _isConnectHandlersAttached;

		private bool _isQueueCleared;

		private bool _isInternalQueueCleared;

		private void Consume<TMessage>(string queueName, Action<TMessage> action, Action<Func<IModel>> configure)
		{
			_queueName = queueName;
			_logger?.Info($"Start Subsciribe {_queueName}");
			var getChannel = () => _simpleConnection.CreateModel(queueName);
			_logger?.Info($"Add {queueName}");

			if (_messageHandlerSettings.ClearAfterStart && !_isInternalQueueCleared)
			{
				// Local copies are dropped before they are queued for processing.
				_internalBus.Clear(queueName);
				_isInternalQueueCleared = true;
			}

			// Consume runs again after every reconnect: handlers are attached once,
			// and the queue is purged only on the first start, not on each reconnect.
			if (!_isConnectHandlersAttached)
			{
				_isConnectHandlersAttached = true;
				_simpleConnection.BeforeConnect += () =>
				{
					_logger?.Info($"BeforeConnect {queueName}");
				};
				_simpleConnection.AfterConnect += () =>
				{
					if (_messageHandlerSettings.UseExchange)
						ConfigureExchange<TMessage>(_queueName, _messageHandlerSettings);
					_logger?.Info($"AfterConnect {_queueName}");
					if (_messageHandlerSettings.ClearAfterStart && !_isQueueCleared)
					{
						try
						{
							getChannel.Invoke().QueuePurge(_queueName);
							_isQueueCleared = true;
						}
						catch (Exception ex)
						{
							_logger?.Error($"Try to clean Message:{ex.Message}", ex);
						}
					}
				};
			}

			QueueingBasicConsumer(queueName, action, getChannel, configure);
			// событие соединение с RMQ

			// выполняем подписку
			//_simpleConnection.SubscribeAll();

			_logger?.Info($"Subsciribe {queueName}");
		}

		private void ConfigureExchange<TMessage>(string queueName, IMessageHandlerSettings settings)
		{
			_logger?.Info($"Start ConfigureExchange {queueName}");
			var action = GetExchangeConfigure<TMessage>(queueName, settings);
			_simpleConnection.Configure(GetBindKey(queueName), action);
			_logger?.Info($"End ConfigureExchange {queueName}");
		}

		private Action<Func<IModel>> GetExchangeConfigure<TMessage>(string queueName, IMessageHandlerSettings settings)
		{
			var routingKey = string.Empty;
			IDictionary<string, object> dictionary = null;
			if (settings != null)
			{
				routingKey = settings.RoutingKey;
				dictionary = GetArguments(settings.Ttl);
			}
			var type = fanout;
			if (!string.IsNullOrEmpty(routingKey))
				type = topic;

			Action<Func<IModel>> action = getModel =>
			{
				var exchangeName = _busNaming.GetExchangeName<TMessage>();
				getModel().ExchangeDeclare(exchangeName, type, true);
				getModel().QueueDeclare(queueName, true, false, _messageHandlerSettings.AutoDelete, dictionary);
				getModel().QueueBind(queueName, exchangeName, routingKey ?? String.Empty);
				getModel().BasicQos(0, _messageHandlerSettings?.PrefetchCount ?? 5, false);

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

		private const int SleepTime = 500;
		private int _iteration = 1;

		private void QueueingBasicConsumer<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel,
			Action<Func<IModel>> configure)
		{
			if (!_messageHandlerSettings.TurnOffInternalQueue)
				ProcessFromLocalBus(queueName, action, getChannel);


			_simpleConnection.Execute(_ =>
			{
				if (_consumer != null && !getChannel().IsClosed)
					return;

				configure.Invoke(getChannel);
				var consumer = new EventingBasicConsumer(getChannel.Invoke());

				// Handlers are attached before BasicConsume, otherwise the first
				// deliveries arrive with no handler and stay unacked.
				if (!_messageHandlerSettings.TurnOffInternalQueue)
					consumer.Received += consumerOnReceived(queueName, action, getChannel);
				else
					consumer.Received += consumerOnReceivedOnlyRmq(queueName, action, getChannel);
				consumer.Shutdown += _consumer_Shutdown;
				_consumer = consumer;

				_logger?.Info($"Start BasicConsume {_queueName};{_messageHandlerSettings.DegreeOfParallelism}");
				getChannel().BasicConsume(queueName, false, consumer);
				_logger?.Info($"End BasicConsume {_queueName};{_messageHandlerSettings.DegreeOfParallelism}");
			});

			if (_queue.Count > 0 && GetWorkers().Length < _messageHandlerSettings.DegreeOfParallelism)
			{
				_logger?.Info($"Add default worker {_queueName}");
				AddAWorker();
			}
		}

		private void _consumer_Shutdown(object sender, ShutdownEventArgs e)
		{
			if (_consumer != null)
				_consumer.Shutdown -= _consumer_Shutdown;
			_consumer = null;
			if (_isTerminating)
				return;
			_simpleConnection.Close();
			Shutdown?.Invoke();
		}

		private void ProcessFromLocalBus<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel)
		{
			try
			{

				var items = _internalBus.Get(queueName);
				if (items.FastAny())
				{
					Parallel.ForEach(items, item =>
					{
						Action a = () =>
						{
							Increment();
							try
							{
								ProcessMessageInternalBus(queueName, action, getChannel, item, default(TMessage));
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
					});
				}
			}
			catch (Exception e)
			{
				_logger?.Error(e.Message, e);
			}
		}

		private static string _retrycount = "retryCount";
		private byte[] ProcessMessageInternalBus<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel, InternalBusItem item, TMessage message)
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
				_logger?.Debug($"Invoke task messageId:{item.Id} queueName={queueName}");
				action.Invoke(message);
				_internalBus.Ack(item.Id);
				_logger?.Debug($"Ack messageId:{item.Id}  queueName={queueName}");
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
						SendErrorMessage(getChannel, queueName, messageArray, exception);
					}
					else
					{
						retryCount++;
						headers[_retrycount] = retryCount;
						Send(getChannel, queueName, messageArray, headers);
					}
				}
				else
				{
					//Send(getChannel, queueName, messageArray, headers);
					SendErrorMessage(getChannel, queueName, messageArray, exception);
				}
				_internalBus.Ack(item.Id);
			}

			return messageArray;
		}
		private readonly object _syncTask = new object();
		private readonly List<AsyncWorker> _asyncWorkers = new List<AsyncWorker>(5);
		private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

		private void ActionOnModel(Func<IModel> channel, Action<IModel> action)
		{
			// The channel is shared by the consumer, the workers and the publishers,
			// so every call goes through the connection lock.
			_simpleConnection.Execute(_ => action.Invoke(channel()));
		}

		private AsyncWorker[] GetWorkers()
		{
			lock (_syncTask)
			{
				return _asyncWorkers.ToArray();
			}
		}

		private bool TryAck(Func<IModel> getChannel, IModel deliveryModel, ulong deliveryTag)
		{
			var isAcked = false;
			try
			{
				ActionOnModel(getChannel, channel =>
				{
					// A delivery tag is valid only on its own channel. After a reconnect
					// the broker redelivers the message, so a stale tag is not acked.
					if (deliveryModel != null && !ReferenceEquals(channel, deliveryModel))
						return;
					if (!channel.IsOpen)
						return;
					channel.BasicAck(deliveryTag, false);
					isAcked = true;
				});
			}
			catch (Exception ex)
			{
				_logger?.Error($"Can not ack message {deliveryTag} queueName={_queueName}: {ex.Message}", ex);
			}

			return isAcked;
		}

		private EventHandler<BasicDeliverEventArgs> consumerOnReceived<TMessage>(string queueName, Action<TMessage> action, Func<IModel> model)
		{
			return (ch, e) =>
			{
				_logger?.Debug($"Received message {_queueName}; internal Queue messages count: {_internalBus.Count()}");

				var deliveryModel = (ch as IBasicConsumer)?.Model;
				try
				{
					var activeCount = GetWorkers().Count(x => x.IsActive);
					if (activeCount >= _messageHandlerSettings.DegreeOfParallelism || (_messageHandlerSettings.DegreeOfParallelism == 1 && _queue.Count == 0))
					{
						_logger?.Debug($"To many threads Sleep {_queueName};{activeCount}>{_messageHandlerSettings.DegreeOfParallelism} ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}");
						ProcessSingleRmqMessage(_queueName, action, model, deliveryModel, e);
						return;
					}

					if (EnqueueToInternalBus(queueName, action, model, deliveryModel, e))
						ManageWorkers();
				}
				catch (System.TimeoutException te)
				{
					_logger?.Error(te.Message, te);
					UnsubscribeFromQueue(queueName, action, model);
					throw;
				}
				catch (System.IO.EndOfStreamException endOfStreamException)
				{
					_logger?.Error(endOfStreamException.Message, endOfStreamException);
					UnsubscribeFromQueue(queueName, action, model);
					throw;
				}
				catch (OperationInterruptedException ex)
				{
					_logger?.Error(ex.Message, ex);
					Thread.Sleep(SleepTime);
				}
				catch (Exception ex)
				{
					_logger?.Error(ex.Message, ex);
				}
			};
		}

		private bool EnqueueToInternalBus<TMessage>(
			string queueName,
			Action<TMessage> action,
			Func<IModel> model,
			IModel deliveryModel,
			BasicDeliverEventArgs e)
		{
			var headers = e.BasicProperties.Headers ?? new Dictionary<string, object>();
			var messageAsString = Encoding.UTF8.GetString(e.Body.ToArray());
			try
			{
				var message = _serializeService.Deserialize<TMessage>(messageAsString);
				var item = _internalBus.Register(e, queueName, messageAsString);
				if (!TryAck(model, deliveryModel, e.DeliveryTag))
				{
					// The broker will redeliver the message, drop the local copy.
					_internalBus.Ack(item.Id);
					return false;
				}

				_queue.Enqueue(() =>
				{
					Increment();
					try
					{
						ProcessMessageInternalBus(queueName, action, model, item, message);
					}
					catch (Exception ex)
					{
						_logger?.Error($"Error queueName={queueName}: {ex.Message}", ex);
					}
					finally
					{
						Decrement();
					}
				});
				return true;
			}
			catch (JsonException jre)
			{
				// A malformed message goes to the error queue instead of an endless requeue.
				_logger?.Error($"{jre.Message};{System.Environment.NewLine}JSON:{messageAsString}", jre);
				ActionOnModel(model, channel => SendErrorMessage(channel, queueName, messageAsString, jre));
			}
			catch (Exception ex)
			{
				ProcessException<TMessage>(queueName, model, ex, messageAsString, headers);
			}

			// The message was moved to the error queue or republished for retry.
			TryAck(model, deliveryModel, e.DeliveryTag);
			return false;
		}

		private void ManageWorkers()
		{
			var workers = GetWorkers();
			var tasksCount = workers.Length;
			var freeList = workers.Where(x => x.CanFree).ToList();
			var times = tasksCount << 3;
			if (freeList.Count == 0
				&& times < _queue.Count
				&& tasksCount < _messageHandlerSettings.DegreeOfParallelism)
				AddAWorker();

			if (tasksCount <= 1 || tasksCount <= _queue.Count)
				return;

			_logger?.Info($"Message Count:{_queue.Count};Thread Count:{tasksCount}");
			foreach (var asyncWorker in freeList)
			{
				if (asyncWorker.CanFree)
				{
					asyncWorker.Dispose();
					RemoveAWoker(asyncWorker);
				}
			}
		}

		private void RemoveAWoker(AsyncWorker asyncWorker)
		{
			lock (_syncTask)
			{
				asyncWorker.Stop();
				_asyncWorkers.Remove(asyncWorker);
			}
		}

		private void AddAWorker()
		{
			lock (_syncTask)
			{
				_asyncWorkers.Add(new AsyncWorker(_logger, _queue, RemoveAWoker, _queueName));
			}
		}

		private void UnsubscribeFromQueue<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel)
		{
			_logger?.Debug($"UnsubscribeFromQueue {_queueName};{_messageHandlerSettings.DegreeOfParallelism}");
			//_consumer.Received -= consumerOnReceived(queueName, action, getChannel);
			//_consumer = null;
		}

		private void ProcessException<TMessage>(string queueName, Func<IModel> getChannel, Exception ex, string messageAsString,
			IDictionary<string, object> headers)
		{
			if (ex is AggregateException)
			{
				foreach (var innerException in ((AggregateException)ex).Flatten().InnerExceptions)
				{
					_logger?.Error(
						$"Error queueName={queueName}: {innerException.Message};{System.Environment.NewLine}JSON:{messageAsString}",
						innerException);
				}
			}
			else
			{
				_logger?.Error($"Error queueName={queueName}: {ex.Message};{System.Environment.NewLine}JSON:{messageAsString}",
					ex);
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
					ActionOnModel(getChannel, channel => SendErrorMessage(channel, queueName, messageAsString, ex));
				}
				else
				{
					retryCount++;
					headers["retryCount"] = retryCount;
					ActionOnModel(getChannel, channel => Send(channel, queueName, messageAsString, headers));
				}
			}
			else
			{
				ActionOnModel(getChannel, channel => SendErrorMessage(channel, queueName, messageAsString, ex));
			}
		}


		private EventHandler<BasicDeliverEventArgs> consumerOnReceivedOnlyRmq<TMessage>(string queueName, Action<TMessage> action, Func<IModel> getChannel)
		{
			return (ch, e) =>
			{
				var deliveryModel = (ch as IBasicConsumer)?.Model;
				var activeCount = GetWorkers().Count(x => x.IsActive);
				while (activeCount >= _messageHandlerSettings.DegreeOfParallelism)
				{
					Thread.Sleep(0);
					_logger?.Debug($"To many threads Sleep {_queueName};{activeCount}>{_messageHandlerSettings.DegreeOfParallelism}");
					activeCount = GetWorkers().Count(x => x.IsActive);
				}
				Task.Factory.StartNew(() => ProcessSingleRmqMessage(queueName, action, getChannel, deliveryModel, e));
			};
		}

		private void ProcessSingleRmqMessage<TMessage>(
			string queueName,
			Action<TMessage> action,
			Func<IModel> getChannel,
			IModel deliveryModel,
			BasicDeliverEventArgs e)
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
					action.Invoke(message);
				}
				catch (JsonException jre)
				{
					// A malformed message goes to the error queue instead of an endless requeue.
					_logger?.Error($"{jre.Message};{System.Environment.NewLine}JSON:{messageAsString}", jre);
					ActionOnModel(getChannel, channel => SendErrorMessage(channel, queueName, messageAsString, jre));
				}

				catch (Exception ex)
				{
					ProcessException<TMessage>(queueName, getChannel, ex, messageAsString, headers);
				}
			}
			catch (System.TimeoutException te)
			{
				_logger?.Error(te.Message, te);
				//_consumer.Received -= consumerOnReceivedOnlyRmq(queueName, action, getChannel);
				//_consumer = null;
				throw;
			}
			catch (System.IO.EndOfStreamException endOfStreamException)
			{
				_logger?.Error(endOfStreamException.Message, endOfStreamException);
				//_consumer.Received -= consumerOnReceivedOnlyRmq(queueName, action, getChannel);
				//_consumer = null;
				throw;
			}
			catch (OperationInterruptedException ex)
			{
				_logger?.Error(ex.Message, ex);
				Thread.Sleep(SleepTime);
			}
			catch (Exception ex)
			{
				_logger?.Error(ex.Message, ex);
			}
			finally
			{
				TryAck(getChannel, deliveryModel, e.DeliveryTag);
			}
		}

		private void SendErrorMessage(Func<IModel> channel, string queueName, byte[] body, Exception ex)
		{
			var errorQueue = queueName + ".Error";
			var dictionary = new Dictionary<string, object>(2)
			{
			};
			dictionary["error"] = ex.ToString();
			Send(channel, errorQueue, body, dictionary);
		}
		private void Send(Func<IModel> channel, string queue, byte[] body, IDictionary<string, object> headers)
		{
			try
			{
				ActionOnModel(channel, model =>
				{
					var dictionary = GetArguments(_messageHandlerSettings.Ttl);
					model.QueueDeclare(queue, true, false, false, dictionary);
					var props = model.CreateBasicProperties();
					props.DeliveryMode = 2;
					props.Headers = headers;
					model.BasicPublish("", queue, props, body);

				});

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


		private void Send(IModel channel, string queue, string message, IDictionary<string, object> headers)
		{
			try
			{

				var dictionary = GetArguments(_messageHandlerSettings.Ttl);
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

		private volatile bool _isTerminating;

		/// <inheritdoc />
		public bool IsModel => (_consumer?.Model?.IsOpen ?? false) && (_consumer.IsRunning);

		/// <inheritdoc />
		public void Dispose()
		{
			// Set before closing, so the consumer shutdown does not trigger a resubscribe.
			_isTerminating = true;
			Shutdown = null;

			var consumer = _consumer;
			_consumer = null;
			if (consumer != null)
				consumer.Shutdown -= _consumer_Shutdown;

			foreach (var worker in GetWorkers())
				RemoveAWoker(worker);

			_simpleConnection.Dispose();
		}

		private static string GetQueueKey(string queue) => $"queue:{queue}";

		private static string GetExchangeKey(string exchange) => $"exchange:{exchange}";

		private static string GetBindKey(string queue) => $"bind:{queue}";

		private void DeleteExchange(IModel channel, string exchangeName)
		{
			try
			{
				channel.ExchangeDelete(exchangeName, true);
			}
			catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 406)
			{
				_logger?.Warn($"Exchange {exchangeName} is in use and was not deleted: {ex.Message}");
			}
		}

		/// <inheritdoc />
		public void Delete<TMessage>() where TMessage : class
		{
			_logger?.Info($"Delete");
			var exchangeName = _busNaming.GetExchangeName<TMessage>();
			_simpleConnection.Execute(getChannel => DeleteExchange(getChannel(), exchangeName));
			_simpleConnection.RemoveConfiguration(GetExchangeKey(exchangeName));
		}

		/// <inheritdoc />
		public List<TMessage> GetMessageExchange<TMessage>(string queueName)
		{
			return GetMessageExchange<TMessage>(queueName, 20);
		}

		/// <inheritdoc />
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


		/// <inheritdoc />
		public void Send<TMessage>(TMessage message, int? ttl = null) where TMessage : class
		{
			var queueName = _busNaming.GetQueueName<TMessage>();
			Send(queueName, message, ttl);

		}

		/// <inheritdoc />
		public void Send<TMessage>(string queue, TMessage message, int? ttl = null) where TMessage : class
		{
			_simpleConnection.Configure(GetQueueKey(queue), getChannel =>
			{
				try
				{
					getChannel().QueueDeclare(queue, true, false, false, null);
				}
				catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 406)
				{
					// The queue already exists with other arguments (e.g. x-message-ttl): use it as is.
					_logger?.Warn($"Queue {queue} exists with other arguments: {ex.Message}");
				}
			}, true);

			var body = Encoding.UTF8.GetBytes(_serializeService.Serialize(message));
			_simpleConnection.Execute(getChannel =>
			{
				var props = getChannel().CreateBasicProperties();
				props.DeliveryMode = 2;

				// Per-message TTL: the queue is not deleted and redeclared any more.
				if (ttl.HasValue)
					props.Expiration = ttl.Value.ToString(CultureInfo.InvariantCulture);
				getChannel().BasicPublish("", queue, props, body);
			});
		}

		/// <inheritdoc />
		public void Publish<TMessage>(TMessage message) where TMessage : class
		{
			Publish(message, String.Empty);
		}

		private static string fanout = "fanout";
		private static string topic = "topic";

		/// <inheritdoc />
		public void Publish<TMessage>(TMessage message, string routingKey, string exchangeName = null) where TMessage : class
		{
			var type = fanout;
			if (!string.IsNullOrEmpty(routingKey))
				type = topic;


			exchangeName = exchangeName ?? _busNaming.GetExchangeName<TMessage>();
			_simpleConnection.Configure(GetExchangeKey(exchangeName), getChannel =>
			{
				getChannel().ExchangeDeclare(exchangeName, type, true, arguments: null);
			});

			var body = Encoding.UTF8.GetBytes(_serializeService.Serialize(message));
			_simpleConnection.Execute(getChannel =>
			{
				var props = getChannel().CreateBasicProperties();
				props.DeliveryMode = 2;
				getChannel().BasicPublish(exchangeName, routingKey ?? String.Empty, props, body);
			});
		}

		/// <inheritdoc />
		public void Delete<TMessage>(string queue) where TMessage : class
		{
			var exchangeName = _busNaming.GetExchangeName<TMessage>();
			_simpleConnection.Execute(getChannel =>
			{
				DeleteExchange(getChannel(), exchangeName);
				getChannel().QueueDelete(queue);
			});
			_simpleConnection.RemoveConfiguration(GetExchangeKey(exchangeName));
			_simpleConnection.RemoveConfiguration(GetQueueKey(queue));
			_simpleConnection.RemoveConfiguration(GetBindKey(queue));
		}

		/// <inheritdoc />
		public void Delete(string queue)
		{
			_simpleConnection.Execute(getChannel =>
			{
				getChannel().QueueDelete(queue);
			});
			_simpleConnection.RemoveConfiguration(GetQueueKey(queue));
			_simpleConnection.RemoveConfiguration(GetBindKey(queue));
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

		/// <inheritdoc />
		public bool IsConnected
		{
			get { return _simpleConnection.IsConnected; }
		}

		/// <inheritdoc />
		public void Reconnect(string? name = null)
		{
			try
			{
				_simpleConnection.Reconnect(name);

			}
			catch
			{
				Thread.Sleep(SleepTime);
			}

		}
		/// <inheritdoc />
		public void Purge(string queueName)
		{
			_simpleConnection.Execute(getChannel =>
			{
				try
				{
					getChannel().QueuePurge(queueName);
				}
				catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
				{
					_logger.Warn($"Queue:{queueName} does not exists. Message:{ex.Message}");
				}
			});
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
			where TRequest : class
			where TResponse : class
		{
			return Respond<TRequest, TResponse>(response, null);
		}


		private string GetQueue<TRequest>() where TRequest : class
		{
			return _messageHandlerSettings != null ? _messageHandlerSettings.SubscriptionEndpoint ?? _busNaming.GetQueueName<TRequest>() : _busNaming.GetQueueName<TRequest>();
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