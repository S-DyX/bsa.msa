using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Services.Settings;
using Bsa.Msa.RabbitMq.Core;
using Bsa.Msa.RabbitMq.Core.Common;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.Common.Services.Impl
{
	/// <inheritdoc />
	public sealed class ServiceUnitManager : IServiceUnitManager
	{
		private readonly ISimpleBus _simpleBus;
		private readonly IServicesSettings _servicesSection;
		private readonly ICommandFactory _commandFactory;
		private readonly IRepeaterFactory _repeaterFactory;
		private readonly ISubscriberFactory _subscriberFactory;
		private readonly ILocalLogger _logger;
		private readonly IMessageHandlerFactory _messageHandlerFactory;
		private readonly List<ISubscriber> _subscribers = new List<ISubscriber>();
		private readonly List<MessageHandlerSettings> _deleteQueue = new List<MessageHandlerSettings>();
		private readonly List<IServiceUnit> _serviceUnits = new List<IServiceUnit>();
		private bool _isStart = false;
		private readonly InternalBus _internalBus;
		private readonly Thread _thread;

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="servicesSection"></param>
		/// <param name="simpleBus"></param>
		/// <param name="commandFactory"></param>
		/// <param name="repeaterFactory"></param>
		/// <param name="subscriberFactory"></param>
		/// <param name="messageHandlerFactory"></param>
		public ServiceUnitManager(IServicesSettings servicesSection,
			ISimpleBus simpleBus,
			ICommandFactory commandFactory,
			IRepeaterFactory repeaterFactory,
			ISubscriberFactory subscriberFactory,
			IMessageHandlerFactory messageHandlerFactory)
		: this(servicesSection, simpleBus, commandFactory, repeaterFactory, subscriberFactory, messageHandlerFactory, null, null)
		{
			_simpleBus = simpleBus;
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="servicesSection"></param>
		/// <param name="simpleBus"></param>
		/// <param name="commandFactory"></param>
		/// <param name="repeaterFactory"></param>
		/// <param name="subscriberFactory"></param>
		/// <param name="messageHandlerFactory"></param>
		/// <param name="serializeService"></param>
		/// <param name="localLogger"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public ServiceUnitManager(IServicesSettings servicesSection,
			ISimpleBus simpleBus,
			ICommandFactory commandFactory,
			IRepeaterFactory repeaterFactory,
			ISubscriberFactory subscriberFactory,
			IMessageHandlerFactory messageHandlerFactory,
			ISerializeService serializeService,
			ILocalLogger localLogger)
		{
			if (servicesSection == null) throw new ArgumentNullException("servicesSection");

			_thread = new Thread(Check);
			_thread.Start();
			_servicesSection = servicesSection;
			_commandFactory = commandFactory;
			_repeaterFactory = repeaterFactory;
			_subscriberFactory = subscriberFactory;
			_logger = localLogger;
			_messageHandlerFactory = messageHandlerFactory;

			int minWorker, minIoc;
			ThreadPool.GetMinThreads(out minWorker, out minIoc);
			if (ThreadPool.SetMinThreads(minWorker * 4, minIoc * 4))
			{
				// The minimum number of threads was set successfully.
			}
			ThreadPool.GetMaxThreads(out minWorker, out minIoc);
			if (ThreadPool.SetMaxThreads(minWorker * 4, minIoc * 4))
			{
				// The minimum number of threads was set successfully.
			}
			serializeService = serializeService ?? new SerializeService();
			_internalBus = InternalBus.Create(serializeService, _logger);
		}

		private void Check()
		{
			while (_isStart)
			{
				try
				{
					foreach (var subscriber in _subscribers)
					{
						_logger.Info($"IsStarted:{subscriber.IsStarted}; {subscriber.Name}");
						if (!subscriber.IsStarted)
							;
					}
					Thread.Sleep(3000);
				}
				catch (Exception e)
				{
					_logger.Error(e.Message,e);
				}
			}
		}

		/// <inheritdoc />
		public void Start()
		{
			_logger?.Info($"Start load local bus");
			_internalBus.Load();
			_logger?.Info($"End load local bus");
			var services = _servicesSection.GetServices().ToArray();
			_logger?.Info($"Start load services ");
			foreach (var service in services)
			{
				_logger?.Info($"Parse service section {service.Name}");
				var handlers = service.GetHandlers();
				foreach (var handler in handlers)
				{
					CreateNew(handler);
				}
				var commands = service.GetCommands();
				foreach (var commandSetting in commands)
				{
					var rep = new RepeatingCommandContainer(commandSetting, _repeaterFactory, _commandFactory);
					rep.OnError += HandleServiceUnitError;
					_serviceUnits.Add(rep);
					//list.Add(_repeaterFactory.Create(commandSetting.Type, commandSetting));
				}
			}

			_logger?.Info($"End load services {services.Length}");
			Parallel.ForEach(_deleteQueue, new ParallelOptions() { MaxDegreeOfParallelism = 2 }, x =>
			{
				_subscriberFactory.Delete(x);
			});
			Parallel.ForEach(_serviceUnits, new ParallelOptions(){MaxDegreeOfParallelism = 2}, x =>
			{
				x.Start();
			});
			Parallel.ForEach(_subscribers, new ParallelOptions() { MaxDegreeOfParallelism = 2 }, x =>
			{
				x.Start();
			});
		}

		private void CreateNew(MessageHandlerSettings handler)
		{
			try
			{
				if (handler.DegreeOfParallelism > 0)
				{
					//for (var index = 0; index < handler.DegreeOfParallelism; index++)
					{
						var sub = _subscriberFactory.Create(handler, _messageHandlerFactory);
						sub.OnError += HandleServiceUnitError;
						_subscribers.Add(sub);

					}
				}
				else
				{
					_deleteQueue.Add(handler);
				}
			}
			catch (Exception e)
			{
				_logger.Error($"Can not create handler: {handler.Name} Message:{e.Message}", e);
			}
			

			_logger?.Info($"Created: Subscriber:{handler.Type}, Count:{handler.DegreeOfParallelism}");
		}

		/// <inheritdoc />
		public void Stop()
		{
			foreach (var sub in _serviceUnits)
			{
				sub.Stop();
			}
			foreach (var sub in _subscribers)
			{
				sub.Stop();
			}
			_subscribers.Clear();
			_serviceUnits.Clear();
			_isStart = false;
		}

		/// <inheritdoc />
		public event UnhandledExceptionEventHandler OnError;

		private void HandleServiceUnitError(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = (Exception)e.ExceptionObject;
			_logger?.Error(ex.InnerException?.Message ?? ex.Message, ex);
			OnError?.Invoke(sender, e);
		}

		/// <inheritdoc />
		public void Paused()
		{
		}
		/// <inheritdoc />
		public void Continued()
		{
		}
	}
}
