using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using FileStorage.Contracts.Rest.Impl.FileStorage;
using Service.Registry.Common;

namespace Bsa.Msa.Example.Host.Handlers
{
	public sealed class ExampleExceptionMessage
	{
	}

	public sealed class ExampleExceptionMessageHandler : IMessageHandler<ExampleExceptionMessage>
	{
		private readonly IBusManager _busManager;
		private readonly ISettings _settings;
		private readonly IServiceRegistryFactory _serviceRegistryFactory;

		public ExampleExceptionMessageHandler(IBusManager busManager, ISettings settings, IServiceRegistryFactory serviceRegistryFactory)
		{
			this._busManager = busManager;
			this._settings = settings;
			_serviceRegistryFactory = serviceRegistryFactory;
		}
		public void Handle(ExampleExceptionMessage message)
		{
			throw new NotImplementedException("Test");
		}
	}
}
