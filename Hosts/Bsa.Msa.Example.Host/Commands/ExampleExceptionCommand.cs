using System;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.MessageHandling.Entities.Ver001;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.Example.Host.Handlers;
using Bsa.Msa.RabbitMq.Core.Interfaces;
namespace Bsa.Msa.Example.Host.Commands
{
	public sealed class ExampleExceptionCommand : ICommand
	{
		private readonly ISingleRmqBus _singleRmqBus;
		private readonly ISimpleMessageHandlerSettigns _settings;

		public ExampleExceptionCommand(ISingleRmqBus singleRmqBus, ISettings settings)
		{
			_singleRmqBus = singleRmqBus;
			_settings = settings.As();
		}


		public void Execute()
		{
			throw new NotImplementedException("Test");
			var exampleMessage = new ExampleExceptionMessage()
			{
			};

			_singleRmqBus.Send(_settings.PublicationEndpoint ?? string.Empty, exampleMessage);
		}

		public void Dispose()
		{

		}
	}
}
