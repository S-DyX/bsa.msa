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
	public sealed class ExampleCommand : ICommand
	{
		private readonly ISingleRmqBus _singleRmqBus;
		private readonly ISimpleMessageHandlerSettigns _settings;
		private readonly IServiceRegistryFactory _serviceRegistryFactory;
		private readonly string _fileName;
		private readonly string _storage; 

		public ExampleCommand(ISingleRmqBus singleRmqBus, ISettings settings, IServiceRegistryFactory serviceRegistryFactory)
		{
			_singleRmqBus = singleRmqBus;
			_settings = settings.As();
			_serviceRegistryFactory = serviceRegistryFactory;
			_fileName = _settings.GetAttStrValue("fileName", "some.txt");
			_storage = _settings.GetAttStrValue("storage", "test");
			
		}
	

		public void Execute()
		{
            for (int i = 0; i < 10; i++)
            {
                _singleRmqBus.Publish(new EmptyMessage()
                {
                    FileName = $"Name{i}",
                    Hash = "Hash",
                    Name = "Name"
                });
            }
			
			var random = new Random(100); 
			var fileStorageService = _serviceRegistryFactory.CreateRest<IFileStorageRestService, FileStorageRestService>();
			var exampleMessage = new ExampleMessage()
			{
				FileName = _fileName,
				Name = "test",
				Hash = random.NextDouble().ToString()
			};
			if (!fileStorageService.IsExistsByExternalId(exampleMessage.Name, exampleMessage.FileName))
			{
				//_singleRmqBus.Publish(exampleMessage);
				for (int i = 0; i < 10; i++)
				{
					_singleRmqBus.Publish(exampleMessage);

				}
				
				//_singleRmqBus.Publish(new ExampleMessage2()
				//{
				//	FileName = _fileName,
				//	Name = "test"
				//});
			}
		}

		public void Dispose()
		{

		}
	}
}
