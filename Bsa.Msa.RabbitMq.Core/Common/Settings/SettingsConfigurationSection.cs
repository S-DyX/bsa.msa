using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Xml;
using System.Xml.Linq;

namespace Bsa.Msa.Common.Settings
{
	/// <summary>
	/// Represents the settings confguration section.
	/// </summary>
	public class SettingsConfigurationSection :  ISettings
	{

		public SettingsConfigurationSection()
		{
		}

		public SettingsConfigurationSection(IConfigurationSection raw)
		{
			Raw = raw;
            LoadSettings(raw);
		}

		protected virtual void LoadSettings(IConfigurationSection raw)
		{
		}

		/// <summary>
		/// Raw XML of the settings.
		/// </summary>
		public IConfigurationSection Raw { get; private set; }

		

		protected string GetAttValue(IConfigurationSection raw, string name)
		{
			var attribute = raw.GetSection(name);
			if (attribute != null)
				return attribute.Value;
			return string.Empty;
		}
		protected int GetAttIntValue(IConfigurationSection raw, string name, int defaultValue)
		{
			var value = GetAttValue(raw, name);
			if (string.IsNullOrEmpty(value))
				return defaultValue;
			return int.Parse(value);
		}
		protected int? GetAttIntValue(IConfigurationSection raw, string name)
		{
			var value = GetAttValue(raw, name);
			if (string.IsNullOrEmpty(value))
				return null;
			return int.Parse(value);
		}

		protected bool GetAttBoolValue(IConfigurationSection raw, string name, bool defaultValue)
		{
			var value = GetAttValue(raw, name);
			if (string.IsNullOrEmpty(value))
				return defaultValue;
			return bool.Parse(value);
		}
	}
}
