using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using FileStorage.Contracts.Rest.Impl.FileStorage;
using Newtonsoft.Json;
using Service.Registry.Common;
using System;
using System.Threading.Tasks;

namespace Bsa.Msa.Example.Host.Handlers
{



    public sealed class EmptyMessageHandlerAsync : IMessageHandlerAsync<EmptyMessage>
    {
        private readonly IBusManager _busManager;
        private readonly ISettings _settings;
        private readonly IServiceRegistryFactory _serviceRegistryFactory;
        private IFileStorageRestService _proxy;

        public EmptyMessageHandlerAsync(IBusManager busManager, ISettings settings, IServiceRegistryFactory serviceRegistryFactory)
        {
            this._busManager = busManager;
            this._settings = settings;
            _serviceRegistryFactory = serviceRegistryFactory;
        }
        public async Task HandleAsync(EmptyMessage message)
        {
            Console.Write($"Processed: {JsonConvert.SerializeObject(message)}");
            await Task.Delay(1000);

        }
    }
}
