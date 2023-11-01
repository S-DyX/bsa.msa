using System.Xml.Linq;
using Bsa.Msa.Common.Settings;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.Common.Services.Settings
{
	public class ServiceUnitSettings : SettingsConfigurationSection, IServiceUnitSettings
	{
		public ServiceUnitSettings(IConfigurationSection raw)
			: base(raw)
		{
			this.Type = GetAttValue(raw, "type");
			this.DegreeOfParallelism = GetAttIntValue(raw, "degreeOfParallelism", 1);
			this.Postfix = GetAttValue(raw, "postfix") ?? string.Empty;
		}

		public string Type { get; private set; }

		public int DegreeOfParallelism { get; private set; }
		public string Postfix { get; set; }
	}
}
