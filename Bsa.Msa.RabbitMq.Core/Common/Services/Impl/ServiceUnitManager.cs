using System;
using System.Collections.Generic;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Services.Settings;

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


		public ServiceUnitManager(IServicesSettings servicesSection,
			ILocalLogger localLogger, 
			ICommandFactory commandFactory,
			IRepeaterFactory repeaterFactory,
			ISubscriberFactory subscriberFactory,
			IMessageHandlerFactory messageHandlerFactory)
		{
			if (servicesSection == null) throw new ArgumentNullException("servicesSection");

			_servicesSection = servicesSection;
			_commandFactory = commandFactory;
			_repeaterFactory = repeaterFactory;
			_subscriberFactory = subscriberFactory;
			_logger = localLogger;
			_messageHandlerFactory = messageHandlerFactory;
		}

		public void Start()
		{
			foreach (var service in _servicesSection.GetServices())
			{
				var handlers = service.GetHandlers();
				foreach (var handler in handlers)
				{
					Create(handler);
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


			_serviceUnits.ForEach(x => x.StartAsync());
			_subscribers.ForEach(x => x.StartAsync());
		}

		private void Create(MessageHandlerSettings handler)
		{
			for (var index = 0; index < handler.DegreeOfParallelism; index++)
			{
				var sub = _subscriberFactory.Create(handler.Type, handler, _messageHandlerFactory);
				sub.OnError += HandleServiceUnitError;
				_subscribers.Add(sub);

			}
			_logger.Info($"Created: Subscriber:{handler.Type}, Count:{handler.DegreeOfParallelism}");
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
			_logger.Error(ex.InnerException?.Message ?? ex.Message, ex);
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
