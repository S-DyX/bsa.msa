//using Bsa.Msa.Common;
//using Bsa.Msa.Common.Services.Interfaces;
//using Bsa.Msa.Common.Services.MessageHandling;
//using Bsa.Msa.RabbitMq.Core.Interfaces;
//using System;
//using System.Diagnostics;
//using System.Threading;

//namespace Bsa.Msa.RabbitMq.Core
//{
//	public sealed class SubscriberBaseOld<TMessage> : ISubscriber where TMessage : class
//	{

//		private readonly IMessageHandlerSettings _messageHandlerSettings;

//		private readonly IMessageHandlerFactory _factory;
//		private readonly ISimpleBus _simpleBus;
//		private readonly ILocalBus _localBus;
//		private readonly ILocalLogger _logger;
//		private bool _isRun;
//		private IMessageHandler<TMessage> _messageHandler;

//		private IDisposable _subscribers;
//		private Thread _task;

//		public SubscriberBaseOld(IMessageHandlerSettings messageHandlerSettings,
//			IMessageHandlerFactory factory,
//			ILocalLogger logger,
//			ISimpleBus simpleBus,
//			ILocalBus localBus)
//		{
//			_factory = factory;
//			_simpleBus = simpleBus;
//			_localBus = localBus;
//			_isRun = true;
//			_logger = logger;
//			_messageHandlerSettings = messageHandlerSettings;
//		}

//		private void DisposeInternal()
//		{
//			using (_subscribers) { }
//		}

//		public void Start()
//		{
//			var isInit = false;
//			while (!isInit)
//			{
//				try
//				{
//					Init();
//					isInit = true;
//				}
//				catch (System.IO.EndOfStreamException endOfStreamException)
//				{
//					_simpleBus.Reconnect();
//					_logger?.Error($"EndOfStreamException subscription: {_messageHandlerSettings.Type}", endOfStreamException);
//				}
//				catch (Exception ex)
//				{
//					_logger?.Error($"Cannot start subscription: {_messageHandlerSettings.Type}", ex);
//					OnError?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
//				}
//			}

//		}

//		private void Init()
//		{
//			_logger?.Info($"Start subscription: {_messageHandlerSettings.Type}");
		

//			var subscriptionEndpoint = string.IsNullOrEmpty(_messageHandlerSettings.SubscriptionEndpoint)
//				? SimpleBusExtension.GetQueueName<TMessage>()
//				: _messageHandlerSettings.SubscriptionEndpoint;


//			_messageHandler = _factory.Create<TMessage>(_messageHandlerSettings.Type, _messageHandlerSettings, _simpleBus, _localBus);
//			if (_messageHandlerSettings.UseExchange)
//			{
//				_simpleBus.SubscribeExchange<TMessage>(subscriptionEndpoint,
//					message => _messageHandler.Handle(message), _messageHandlerSettings);
//			}
//			else
//			{
//				_localBus.Register(_messageHandlerSettings);
//				_simpleBus.Subscribe<TMessage>(subscriptionEndpoint,
//					message => _messageHandler.Handle(message), _messageHandlerSettings);
//			}
//			_logger?.Info($"End subscription: {_messageHandlerSettings.Type};"); 

//		}



//		public void Stop()
//		{
//			_isRun = false;
//			DisposeInternal();
//		}

//		public event UnhandledExceptionEventHandler OnError;


//		public void StartAsync()
//		{
//			_task = new Thread(Start);
//			_task.Start();
//		}
//	}
//}
