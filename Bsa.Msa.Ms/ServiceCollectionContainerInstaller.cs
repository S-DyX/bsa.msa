using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Impl;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Services.Settings;
using Bsa.Msa.RabbitMq.Core;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bsa.Msa.DependencyInjection
{
	public static class ServiceCollectionContainerInstaller
	{
		public static void InstallServices(this IServiceCollection builder)
		{
			InstallRabbit(builder);
			InstallHandlers(builder);
			
			builder.AddSingleton<ILocalContainer, LocalContainer>();


		}

		

		private static void InstallHandlers(IServiceCollection builder)
		{
            builder.AddSingleton<ISerializeService, SerializeService>();
            builder.AddSingleton<ILocalContainer, LocalContainer>();
            builder.AddSingleton<IHandlerRegistry, HandlerRegistry>();
            builder.AddSingleton<ICommandRegistry, CommandRegistry>();
            
            builder.AddSingleton<IServiceUnitManager, ServiceUnitManager>();
            builder.AddSingleton<IServicesSettings, ServicesSettings>();
            builder.AddSingleton<ICommandFactory, CommandFactory>();
            builder.AddSingleton<IRepeaterFactory, RepeaterFactory>();
            builder.AddSingleton<IMessageHandlerFactory, MessageHandlerFactory>();
            builder.AddSingleton<ISubscriberFactory, SubscriberFactory>();

		}

		private static void InstallRabbit(IServiceCollection builder)
		{
            builder.AddSingleton<ISingleRmqBus, SingleRmqBus>();
            builder.AddSingleton<IRabbitMqSettings, RabbitMqSettings>();
            builder.AddSingleton<ILocalBus, LocalBus>();
            builder.AddSingleton<IBusManager, BusManager>();
            builder.AddSingleton<ISimpleBus, SimpleBus>();
            builder.AddSingleton<ISimpleConnection, SimpleConnection>();
		}
	}
}
