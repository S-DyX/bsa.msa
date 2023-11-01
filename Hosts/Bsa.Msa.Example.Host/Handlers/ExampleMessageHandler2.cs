using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using FileStorage.Contracts.Rest.Impl.FileStorage;
using Service.Registry.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bsa.Msa.Example.Host.Handlers
{



	public sealed class ExampleMessage2
	{
		public string FileName { get; set; }
		public string Name { get; set; }
	}
	public sealed class ExampleMessageHandler2 : IMessageHandler<ExampleMessage2>
	{
		private readonly IBusManager _busManager;
		private readonly ISettings _settings;
		private readonly IServiceRegistryFactory _serviceRegistryFactory;
		private IFileStorageRestService _proxy;

		public ExampleMessageHandler2(IBusManager busManager, ISettings settings, IServiceRegistryFactory serviceRegistryFactory)
		{
			this._busManager = busManager;
			this._settings = settings;
			_serviceRegistryFactory = serviceRegistryFactory;
			_proxy = _serviceRegistryFactory.CreateTcp<ITcpFileStorageService, TcpFileStorageService>();
		}
		public void Handle(ExampleMessage2 message)
		{
			//System.Threading.Thread.Sleep(2000);
			
			var id = _proxy.GetIdByExternal(message.FileName);
			var bytes = new List<byte>(20000 * 4);
			for (int i = 0; i < 20000; i++)
			{
				var chars = i.ToString();
				bytes.AddRange(Encoding.UTF8.GetBytes(chars));
			}
			_proxy.SaveBytes(id, bytes.ToArray(), message.Name, Guid.NewGuid().ToString());
			var size = _proxy.GetSize(id, message.Name);
		}
	}
}
