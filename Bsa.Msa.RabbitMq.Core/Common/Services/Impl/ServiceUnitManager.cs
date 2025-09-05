using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Services.Settings;
using Bsa.Msa.RabbitMq.Core;
using Bsa.Msa.RabbitMq.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.Common.Services.Impl
{
	public sealed class ServiceUnitManager : IServiceUnitManager
	{
		private readonly IServicesSettings _servicesSection;
		private readonly ICommandFactory _commandFactory;
		private readonly IRepeaterFactory _repeaterFactory;
		private readonly ISubscriberFactory _subscriberFactory;
		private readonly ILocalLogger _logger;
		private readonly IMessageHandlerFactory _messageHandlerFactory;
		private readonly List<ISubscriber> _subscribers = new List<ISubscriber>();
		private readonly List<IServiceUnit> _serviceUnits = new List<IServiceUnit>();
		private bool _isStart = true;
		private readonly InternalBus _internalBus;
		private readonly Thread _thread;
		public ServiceUnitManager(IServicesSettings servicesSection,

			ICommandFactory commandFactory,
			IRepeaterFactory repeaterFactory,
			ISubscriberFactory subscriberFactory,
			IMessageHandlerFactory messageHandlerFactory)
		: this(servicesSection, commandFactory, repeaterFactory, subscriberFactory, messageHandlerFactory, null, null)
		{
		}

		public ServiceUnitManager(IServicesSettings servicesSection,
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
			Parallel.ForEach(_serviceUnits, new ParallelOptions(){MaxDegreeOfParallelism = 2}, x =>
			{
				x.Start();
			});
			Parallel.ForEach(_subscribers, new ParallelOptions() { MaxDegreeOfParallelism = 2 }, x =>
			{
				x.Start();
			});
		}

		private void Create(MessageHandlerSettings handler)
		{
			for (var index = 0; index < handler.DegreeOfParallelism; index++)
			{
				var sub = _subscriberFactory.Create(handler.Type, handler, _messageHandlerFactory);
				sub.OnError += HandleServiceUnitError;
				_subscribers.Add(sub);

			}
			_logger?.Info($"Created: Subscriber:{handler.Type}, Count:{handler.DegreeOfParallelism}");
		}
		private void CreateNew(MessageHandlerSettings handler)
		{
			try
			{
				if (handler.DegreeOfParallelism > 0)
				{
					//for (var index = 0; index < handler.DegreeOfParallelism; index++)
					{
						var sub = _subscriberFactory.Create(handler.Type, handler, _messageHandlerFactory);
						sub.OnError += HandleServiceUnitError;
						_subscribers.Add(sub);

					}
				}
			}
			catch (Exception e)
			{
				_logger.Error($"Can not create handler: {handler.Name} Message:{e.Message}", e);
			}
			

			_logger?.Info($"Created: Subscriber:{handler.Type}, Count:{handler.DegreeOfParallelism}");
		}
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

		//private IEnumerable<IServiceUnit> CreateProcessingUnits(MessageHandlerSettings settings)
		//{
		//	for (var index = 0; index < settings.DegreeOfParallelism; index++)
		//	{
		//		var type = typeof(IServiceUnit).Name + "." + settings.Type;
		//		var serviceUnit = _messageHandlingServiceUnitFactory.Create(type, settings);
		//		serviceUnit.OnError += HandleServiceUnitError;
		//		yield return serviceUnit;
		//	}
		//}

		public event UnhandledExceptionEventHandler OnError;

		private void HandleServiceUnitError(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = (Exception)e.ExceptionObject;
			_logger?.Error(ex.InnerException?.Message ?? ex.Message, ex);
			OnError?.Invoke(sender, e);
		}


		public void Paused()
		{
		}

		public void Continued()
		{
		}
	}
}
