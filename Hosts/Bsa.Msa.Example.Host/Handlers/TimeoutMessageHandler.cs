using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using FileStorage.Contracts.Rest.Impl.FileStorage;
using Newtonsoft.Json;
using Service.Registry.Common;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.Example.Host.Handlers
{



    public sealed class TimeoutMessageHandler : IMessageHandler<EmptyMessage>
    {
        private readonly IBusManager _busManager;
        private readonly ISettings _settings;

        public TimeoutMessageHandler(IBusManager busManager, ISettings settings)
        {
            this._busManager = busManager;
            this._settings = settings;
        }
        public void Handle(EmptyMessage message)
        {
            Console.WriteLine($"{DateTime.UtcNow} Thread:{Thread.CurrentThread.ManagedThreadId} Processed: {JsonConvert.SerializeObject(message)}");
            Thread.Sleep(5000);

            Console.WriteLine($"{DateTime.UtcNow} Thread:{Thread.CurrentThread.ManagedThreadId} Processed: {JsonConvert.SerializeObject(message)}");
            throw new SocketException();

        }
    }
}
