using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using FileStorage.Contracts.Rest.Impl.FileStorage;
using Newtonsoft.Json;
using Service.Registry.Common;
using System;
using System.Threading;

namespace Bsa.Msa.Example.Host.Handlers
{



	public sealed class NothingToDoMessage
	{
		public string FileName { get; set; }
		public string Name { get; set; }

		public string Hash { get; set; }
	}
	public sealed class NothingToDoMessageHandler : IMessageHandler<NothingToDoMessage>
	{
		private readonly IBusManager _busManager;
		private readonly ISettings _settings;
		private readonly IServiceRegistryFactory _serviceRegistryFactory;
		private IFileStorageRestService _proxy;

		public NothingToDoMessageHandler(IBusManager busManager, ISettings settings, IServiceRegistryFactory serviceRegistryFactory)
		{
			this._busManager = busManager;
			this._settings = settings;
			_serviceRegistryFactory = serviceRegistryFactory;
		}
		public void Handle(NothingToDoMessage message)
		{
			Console.WriteLine($"{DateTime.UtcNow} Thread:{Thread.CurrentThread.ManagedThreadId} Processed: {JsonConvert.SerializeObject(message)}");
			Thread.Sleep(300);

		}
	}
}
