using System;
using System.Threading;
using System.Threading.Tasks;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Interfaces;

namespace Bsa.Msa.RabbitMq.Core
{
	/// <summary>
	/// Answers requests (RPC): a request from the subscription queue goes to
	/// <see cref="IMessageHandler{TMessage, TResponse}"/>, the result is sent
	/// to the ReplyTo queue of the request.
	/// </summary>
	/// <typeparam name="TMessage">Request type.</typeparam>
	/// <typeparam name="TResponse">Response type.</typeparam>
	public class RespondBase<TMessage, TResponse> : ISubscriber
		where TMessage : class, new()
		where TResponse : class
	{
		private readonly IBusManager _busManager;

		private readonly IMessageHandlerSettings _messageHandlerSettings;

		private readonly IMessageHandlerFactory _factory;

		private IMessageHandler<TMessage, TResponse> _messageHandler;

		private IDisposable _subscription;

		private Task _task;

		private volatile bool _isStarted;

		/// <summary>
		/// Creates the responder.
		/// </summary>
		/// <param name="busManager">Message bus.</param>
		/// <param name="messageHandlerSettings">Handler settings.</param>
		/// <param name="factory">Handler factory.</param>
		public RespondBase(IBusManager busManager, IMessageHandlerSettings messageHandlerSettings, IMessageHandlerFactory factory)
		{
			_busManager = busManager;
			_factory = factory;
			_messageHandlerSettings = messageHandlerSettings;
		}

		/// <inheritdoc />
		public event UnhandledExceptionEventHandler OnError;

		/// <inheritdoc />
		public bool IsStarted => _isStarted;

		/// <inheritdoc />
		public string Name => _messageHandlerSettings.SubscriptionEndpoint;

		/// <summary>
		/// Stops answering requests.
		/// </summary>
		public void Dispose()
		{
			Stop();
		}

		/// <inheritdoc />
		public void Start()
		{
			try
			{
				_messageHandler = _factory.Create<TMessage, TResponse>(
					_messageHandlerSettings.Type,
					_messageHandlerSettings,
					null,
					null);
				_subscription = _busManager.Respond<TMessage, TResponse>(
					request => _messageHandler.Handle(request),
					_messageHandlerSettings.SubscriptionEndpoint);
				_isStarted = true;
			}
			catch (Exception ex)
			{
				OnError?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
			}
		}

		/// <inheritdoc />
		public void Stop()
		{
			_isStarted = false;
			Interlocked.Exchange(ref _subscription, null)?.Dispose();
		}

		/// <inheritdoc />
		public void StartAsync()
		{
			_task = Task.Run(Start);
		}
	}
}
