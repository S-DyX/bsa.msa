using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Bsa.Msa.RabbitMq.Core
{
	public class RespondBase<TMessage, TResponse> : ISubscriber
		where TMessage : class, new()
		where TResponse : class
	{
		private IBusManager _busManager;
		private IMessageHandlerSettings _messageHandlerSettings;
		private IMessageHandlerFactory _factory;
		private IMessageHandler<TMessage, TResponse> _messageHandler;
		private Task _task;


		public RespondBase(IBusManager busManager, IMessageHandlerSettings messageHandlerSettings, IMessageHandlerFactory factory)
		{
			_busManager = busManager;
			_factory = factory;
			_messageHandlerSettings = messageHandlerSettings;

		}

		public void Dispose()
		{
#warning Incorrect task working
			_task = null;
			_messageHandlerSettings = null;
			_busManager = null;
			_factory = null;
			_messageHandler = null;
		}

		//private BlockingCollection<IMessageHandler<TMessage, TResponse>> workers = new BlockingCollection<IMessageHandler<TMessage, TResponse>>();
		public void Start()
		{
			try
			{
				_messageHandler = _factory.Create<TMessage, TResponse>(_messageHandlerSettings.Type, _messageHandlerSettings, null, null);
				//_busManager.Subscribe(bus=>bus.RespondAsync<TMessage, TResponse>(
				//	request =>
				//		Task.Factory.StartNew(() => _messageHandler.Handle(request))));
				//_busManager.Subscribe(bus => bus.Respond<TestMessageRequest, TestMessageResponse>(
				//	request =>new TestMessageResponse()
				//		));
				//var result = _busManager.Request<TestMessageRequest, TestMessageResponse>(new TestMessageRequest());
				//var workers = new BlockingCollection<IMessageHandler<TMessage, TResponse>>();
				//for (int i = 0; i < 1; i++)
				//{
				//	workers.Add(_messageHandler);
				//}
				//// respond to requests
				//_busManager.Subscribe(bus => bus.RespondAsync<TMessage, TResponse>(request =>
				//	Task.Factory.StartNew(() =>
				//	{
				//		var worker = workers.Take();
				//		try
				//		{
				//			return worker.Handle(request);
				//		}
				//		finally
				//		{
				//			workers.Add(worker);
				//		}
				//	})));
				//var result = _busManager.Request<TMessage, TResponse>(new TMessage());
				if (_messageHandlerSettings.UseExchange)
				{
					_busManager.Respond<TMessage, TResponse>(request => _messageHandler.Handle(request), _messageHandlerSettings.SubscriptionEndpoint);
				}
				else
				{
#warning The 'then' statement is equivalent to the 'else' statement!
					_busManager.Respond<TMessage, TResponse>(request => _messageHandler.Handle(request), _messageHandlerSettings.SubscriptionEndpoint);
				}

			}
			catch (Exception ex)
			{
				if (OnError != null)
					OnError(this, new UnhandledExceptionEventArgs(ex, false));
			}

		}

		public void Stop()
		{
			Dispose();
		}

		public event UnhandledExceptionEventHandler OnError;
		public bool IsStarted => true;
		public string Name => _messageHandlerSettings.SubscriptionEndpoint;


		public void StartAsync()
		{
			_task = new Task(Start);
			_task.Start();
		}
	}
}
