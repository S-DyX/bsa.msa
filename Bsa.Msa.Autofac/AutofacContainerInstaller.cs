using Autofac;
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

namespace Bsa.Msa.Autofac
{
	/// <summary>
	/// Extension class for ioc registration
	/// </summary>
	public static class AutofacContainerInstaller
	{
		/// <summary>
		/// We register all the services required for operation in the ioc
		/// </summary>
		/// <param name="builder"></param>
		public static void InstallServices(this ContainerBuilder builder)
		{
			InstallRabbit(builder);
			InstallHandlers(builder);
		}

		

		private static void InstallHandlers(ContainerBuilder builder)
		{
			builder.RegisterType<SerializeService>()
				.As<ISerializeService>()
				.SingleInstance();
			
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
					.RegisterType<MessageHandlerFactory>()
					.As<IMessageHandlerFactory>()
					.SingleInstance();
		

			builder
				.RegisterType<SubscriberFactory>()
				.As<ISubscriberFactory>()
				.InstancePerDependency();
		}

		private static void InstallRabbit(ContainerBuilder builder)
		{
			builder.RegisterType<SingleRmqBus>()
							.As<ISingleRmqBus>()
							.SingleInstance();
		
			builder
				.RegisterType<RabbitMqSettings>()
				.As<IRabbitMqSettings>()
				.SingleInstance();

			builder
				.RegisterType<LocalBus>()
				.As<ILocalBus>()
				.SingleInstance();

			builder.RegisterType<BusManager>()
				.As<IBusManager>()
				.InstancePerDependency();
			builder
				.RegisterType<SimpleBus>()
				.As<ISimpleBus>()
				.InstancePerDependency();

			builder
				.RegisterType<SimpleConnection>()
				.As<ISimpleConnection>()
				.InstancePerDependency();
		}
	}
}
