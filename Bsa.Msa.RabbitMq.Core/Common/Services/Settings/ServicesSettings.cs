using System;
using Bsa.Msa.Common.Settings;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Bsa.Msa.Common.Services.Settings
{
	/// <summary>
	/// Настройки служб
	/// </summary>
	public class ServicesSettings : IServicesSettings
	{

		private readonly Dictionary<string, ServiceSettings> _services = new Dictionary<string, ServiceSettings>();

		public ServicesSettings()
		{
			var jsonConfigurationRoot = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("services.json", optional: true, reloadOnChange: true)
				.Build();
			LoadSettings(jsonConfigurationRoot);
		}

		private void LoadSettings(IConfigurationRoot raw)
		{
			Raw = raw.GetSection("services");
			
			var children = Raw.GetChildren();
			foreach (var configuration in children)
			{
				var name = configuration.GetSection("name")?.Value;
				if (name != null)
				{
					var serviceSettings = new ServiceSettings(name, configuration);

					if (!_services.ContainsKey(name))
						_services.Add(name, serviceSettings);
				}
			}
			
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
