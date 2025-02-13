using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using FileStorage.Contracts.Rest.Impl.FileStorage;
using Newtonsoft.Json;
using Service.Registry.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.Example.Host.Handlers
{



    public sealed class EmptyMessageHandlerAsync : IMessageHandlerAsync<EmptyMessage>
    {
        private readonly IBusManager _busManager;
        private readonly ISettings _settings;

        public EmptyMessageHandlerAsync(IBusManager busManager, ISettings settings)
        {
            this._busManager = busManager;
            this._settings = settings;
        }
        public async Task HandleAsync(EmptyMessage message)
        {
            Console.WriteLine($"{DateTime.UtcNow} Thread:{Thread.CurrentThread.ManagedThreadId} Processed: {JsonConvert.SerializeObject(message)}");
            await Task.Delay(1000);

        }
    }
}
