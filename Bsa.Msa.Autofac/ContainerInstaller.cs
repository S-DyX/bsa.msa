using Autofac;
using Bsa.Msa.Common;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Impl;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Services.Settings;
using Bsa.Msa.DependencyInjection;
using Bsa.Msa.RabbitMq.Core;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bsa.Msa.Autofac
{
	public static class ContainerInstaller
	{

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
		public static void InstallHandlers(this ContainerBuilder builder)
		{
			builder.RegisterType<LocalContainer>()
							.As<ILocalContainer>()
							.SingleInstance();
			builder.RegisterType<HandlerRegistry>()
				.As<IHandlerRegistry>()
				.SingleInstance();

			builder.RegisterType<CommandRegistry>()
				.As<ICommandRegistry>()
				.SingleInstance();

			builder
				.RegisterType<ServiceUnitManager>()
				.As<IServiceUnitManager>()
				.SingleInstance();

			builder
				.RegisterType<ServicesSettings>()
				.As<IServicesSettings>()
				.SingleInstance();
			builder
				.RegisterType<CommandFactory>()
				.As<ICommandFactory>()
				.SingleInstance();




			builder
				.RegisterType<RepeaterFactory>()
				.As<IRepeaterFactory>()
				.SingleInstance();
			builder
				.RegisterType<SubscriberFactory>()
				.As<ISubscriberFactory>()
				.SingleInstance();
			builder
					.RegisterType<MessageHandlerFactory>()
					.As<IMessageHandlerFactory>()
					.SingleInstance();
		}

		public static void InstallRabbit(this ContainerBuilder builder)
		{
			builder.RegisterType<SingleRmqBus>()
							.As<ISingleRmqBus>()
							.SingleInstance();
			builder.RegisterType<BusManager>()
				.As<IBusManager>()
				.SingleInstance();
			builder
				.RegisterType<RabbitMqSettings>()
				.As<IRabbitMqSettings>()
				.SingleInstance();

			builder
				.RegisterType<LocalBus>()
				.As<ILocalBus>()
				.SingleInstance();

			builder
				.RegisterType<SimpleBus>()
				.As<ISimpleBus>()
				.SingleInstance();

			builder
				.RegisterType<SimpleConnection>()
				.As<ISimpleConnection>()
				.SingleInstance();
		}
	}
}
