using Autofac;
using Autofac.Extensions.DependencyInjection;
using Bsa.Msa.Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Service.Registry.Common;
using Bsa.Msa.Common;

namespace Bsa.Msa.Example.Host
{
	class Program
	{
		public static async Task Main(string[] args)
		{
			var provider = new AutofacServiceProviderFactory();

			await new HostBuilder()
				.UseServiceProviderFactory(provider)
				.ConfigureServices(ConfigureServices)
				.ConfigureContainer<ContainerBuilder>(ConfigureContainer)
				.UseConsoleLifetime()
				.Build()
				.RunAsync();
			//.RunConsoleAsync();
		}


		private static void ConfigureServices(IServiceCollection services)
		{
			services.AddHostedService<ServiceHost>();
			services.AddHttpClient();
		}
		

		private static void ConfigureContainer(ContainerBuilder containerBuilder)
		{

			//containerBuilder.RegisterType<LocalLogger>()
			//	.As<ILocalLogger>();
			containerBuilder.RegisterType<ServiceRegistryFactory>()
				.As<IServiceRegistryFactory>();
			containerBuilder.InstallServices();
			containerBuilder.RegisterType<LocalLogger>()
				.As<ILocalLogger>();
			
		}

	}
}
