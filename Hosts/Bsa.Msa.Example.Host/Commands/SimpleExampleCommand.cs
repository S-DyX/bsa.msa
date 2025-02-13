using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.MessageHandling.Entities.Ver001;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.Example.Host.Handlers;
using Bsa.Msa.RabbitMq.Core.Interfaces;

namespace Bsa.Msa.Example.Host.Commands
{
	public sealed class SimpleExampleCommand : ICommand
	{
		private readonly ISingleRmqBus _singleRmqBus;
		private readonly ISimpleMessageHandlerSettigns _settings;

		public SimpleExampleCommand(ISingleRmqBus singleRmqBus, ISettings settings)
		{
			_singleRmqBus = singleRmqBus;
			_settings = settings.As();

		}


		public void Execute()
		{
			for (int i = 0; i < 2; i++)
			{
				_singleRmqBus.Publish(new EmptyMessage()
				{
					FileName = $"Name{i}",
					Hash = "Hash",
					Name = "Name"
				});
			}


		}

		public void Dispose()
		{

		}
	}
}
