using Bsa.Msa.Common;
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
	public static class ContainerInstaller
	{
        public static void InstallServices(this IServiceCollection builder)
        {
            InstallRabbit(builder);
            InstallHandlers(builder);
            


        }
        public static void InstallHandlers(this IServiceCollection services)
		{
            services.AddSingleton<ILocalLogger, LocalLogger>();
            services.AddSingleton<ILocalContainer, LocalContainer>();
			services.AddSingleton<IHandlerRegistry, HandlerRegistry>();
			services.AddSingleton<ICommandRegistry, CommandRegistry>();
			services.AddSingleton<IServiceUnitManager, ServiceUnitManager>();
			services.AddSingleton<IServicesSettings, ServicesSettings>();

			services.AddSingleton<IRepeaterFactory, RepeaterFactory>();
			services.AddSingleton<ICommandFactory, CommandFactory>();
			services.AddSingleton<IMessageHandlerFactory, MessageHandlerFactory>();

			services.AddSingleton<ISubscriberFactory, SubscriberFactory>();
		}

		public static void InstallRabbit(this IServiceCollection services)
		{
			services.AddSingleton<ISingleRmqBus, SingleRmqBus>();
			services.AddSingleton<IRabbitMqSettings, RabbitMqSettings>();
			services.AddSingleton<ILocalBus, LocalBus>();
			services.AddTransient<IBusManager, BusManager>();
			services.AddTransient<ISimpleBus, SimpleBus>();
			services.AddTransient<ISimpleConnection, SimpleConnection>();
		} 

	}
}
