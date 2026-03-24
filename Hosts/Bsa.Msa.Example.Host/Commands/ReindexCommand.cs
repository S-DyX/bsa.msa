using Bsa.Msa.Common.Services.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using Bsa.Msa.Common.Services.MessageHandling.Entities.Ver001;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.Example.Host.Handlers;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using FileStorage.Contracts.Rest.Impl.FileStorage;
using Service.Registry.Common;

namespace Bsa.Msa.Example.Host.Commands
{
	public sealed class ReindexCommand : ICommand
	{
		private readonly ISingleRmqBus _singleRmqBus;
		private readonly IServiceRegistryFactory _serviceRegistryFactory;

		public ReindexCommand(ISingleRmqBus singleRmqBus, ISettings settings, IServiceRegistryFactory serviceRegistryFactory)
		{
			_singleRmqBus = singleRmqBus;
			_serviceRegistryFactory = serviceRegistryFactory;
			
		}
	

		public void Execute()
		{
			var exampleMessage = new ReindexMessage()
			{ 
				Body = new string('c', 29698984)
			}; 
			_singleRmqBus.Publish(exampleMessage);
			
		}

		public void Dispose()
		{

		}
	}
}
