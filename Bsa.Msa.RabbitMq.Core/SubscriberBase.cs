using Bsa.Msa.Common;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using System;
using System.Threading;

namespace Bsa.Msa.RabbitMq.Core
{
	public sealed class SubscriberBase<TMessage> : ISubscriber where TMessage : class
	{

		private readonly IMessageHandlerSettings _messageHandlerSettings;

		private readonly IMessageHandlerFactory _factory;
		private readonly ISimpleBus _simpleBus;
		private readonly ILocalBus _localBus;
		private readonly ISimpleBusNaming _busNaming;
		private readonly ILocalLogger _logger;
		private bool _isRun;
		private IMessageHandler<TMessage> _messageHandler;
		private IMessageHandlerAsync<TMessage> _messageHandlerAsync;

		private IDisposable _subscribers;
		private Thread _task;
		private bool _isInit = false;
		public SubscriberBase(IMessageHandlerSettings messageHandlerSettings,
			IMessageHandlerFactory factory,
			ILocalLogger logger,
			ISimpleBus simpleBus,
			ILocalBus localBus,
			ISimpleBusNaming busNaming = null)
		{
			_factory = factory;
			_simpleBus = simpleBus;
			_localBus = localBus;
			_busNaming = busNaming ?? new DefaultSimpleBusNaming();
			_isRun = true;
			_logger = logger;
			_messageHandlerSettings = messageHandlerSettings;
		}

		private void DisposeInternal()
		{
			using (_subscribers) { }
		}

		public void Start()
		{
			_isInit = false;
			while (!_isInit)
			{
				try
				{
					Init();
					_isInit = true;
				}
				catch (System.IO.EndOfStreamException endOfStreamException)
				{
					Thread.Sleep(200);
					_simpleBus.Reconnect();
					_logger?.Error($"EndOfStreamException subscription: {_messageHandlerSettings.Type}", endOfStreamException);
				}
				catch (Exception ex)
				{
					Thread.Sleep(200);
					_logger?.Error($"Cannot start subscription: {_messageHandlerSettings.Type}", ex);
					OnError?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
				}

			}

		}

		private void Init()
		{
			_logger?.Info($"Start subscription: {_messageHandlerSettings.Type}");


			var subscriptionEndpoint = string.IsNullOrEmpty(_messageHandlerSettings.SubscriptionEndpoint)
				? _busNaming.GetQueueName<TMessage>()
				: _messageHandlerSettings.SubscriptionEndpoint;


			var messageHandler = _factory.Create<TMessage>(_messageHandlerSettings.Type, _messageHandlerSettings, _simpleBus, _localBus);
			_messageHandler = messageHandler as IMessageHandler<TMessage>;
			_messageHandlerAsync = messageHandler as IMessageHandlerAsync<TMessage>;
			if (_messageHandler != null)
			{

				if (_messageHandlerSettings.UseExchange)
				{
					_simpleBus.SubscribeExchange<TMessage>(subscriptionEndpoint,
						message => _messageHandler.Handle(message), _messageHandlerSettings);
				}
				else
				{
					_localBus.Register(_messageHandlerSettings);
					_simpleBus.Subscribe<TMessage>(subscriptionEndpoint,
						message => _messageHandler.Handle(message), _messageHandlerSettings);
				}
			}
			else if (_messageHandlerAsync != null)
			{
				if (_messageHandlerSettings.UseExchange)
				{
					_simpleBus.SubscribeExchange<TMessage>(subscriptionEndpoint,
						message => _messageHandlerAsync.HandleAsync(message).Wait(), _messageHandlerSettings);
				}
				else
				{
					_localBus.Register(_messageHandlerSettings);
					_simpleBus.Subscribe<TMessage>(subscriptionEndpoint,
						message => _messageHandlerAsync.HandleAsync(message).Wait(), _messageHandlerSettings);
				}
			}
			else
			{
				throw new InvalidOperationException($"Invalid message handler: {_messageHandlerSettings.Type};{_messageHandlerSettings.SubscriptionEndpoint}");
			}
			_logger?.Info($"End setup subscription: {_messageHandlerSettings.Type};");



		}



		public void Stop()
		{
			_isRun = false;
			DisposeInternal();
		}

		public event UnhandledExceptionEventHandler OnError;
		public bool IsStarted => _isInit;


		public void StartAsync()
		{
			_task = new Thread(Start);
			_task.Start();
		}
	}
}
