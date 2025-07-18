using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Bsa.Msa.Common.Services.Settings
{
	/// <summary>
	/// Настройки служб
	/// </summary>
	public sealed class ServicesSettings : IServicesSettings
	{

		private readonly Dictionary<string, ServiceSettings> _services = new Dictionary<string, ServiceSettings>();
		private static readonly string _sectionName = "services";

		private static IConfigurationSection GetConfigurationSection()
		{
			var jsonConfigurationRoot = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("services.json", optional: true, reloadOnChange: true)
				.Build();
			var section = jsonConfigurationRoot.GetSection(_sectionName);
			return section;
		}

		public ServicesSettings()
		{
			var section = GetConfigurationSection();
			LoadSettings(section);
		}

		public ServicesSettings(IConfigurationSection section)
		{
			var services = section.GetSection(_sectionName);
			LoadSettings(services);
		}
		public ServicesSettings(IConfiguration configuration)
		{
			var section = configuration.GetSection(_sectionName);

			if (!LoadSettings(section))
			{
				Console.WriteLine("Not found section");
				section = GetConfigurationSection();
				LoadSettings(section);
				Console.WriteLine($"Services load  {_services.Count}");
			}


		}
		private bool LoadSettings(IConfigurationSection raw)
		{
			if (raw == null)
				return false;
			Raw = raw;
			var found = false;
			var children = Raw.GetChildren();
			foreach (var configuration in children)
			{
				var name = configuration.GetSection("name")?.Value;
				if (name != null)
				{
					var serviceSettings = new ServiceSettings(name, configuration);
					found = true;
					_services.TryAdd(name, serviceSettings);
				}
			}

			return found;
		}

		public IEnumerable<ServiceSettings> GetServices()
		{
			return _services.Values;
		}

		public IConfigurationSection Raw
		{
			get; protected set;
		}
	}
}
