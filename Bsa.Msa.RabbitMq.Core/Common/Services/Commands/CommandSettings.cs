using System;
using System.Xml.Linq;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Settings;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.Common.Services.Commands
{

	public interface ICommandSettings : ISettings
	{ 
		string Name { get;}

		/// <summary>
		/// Тип
		/// </summary>
		 string Type { get; }

		/// <summary>
		/// Тип
		/// </summary>
		 TimeSpan Period { get;}

		 TimeSpan DueTime { get; }

		 RepeaterConcurrentMode Mode { get; }
	}

	/// <summary>
	/// Логическая операция
	/// </summary>
	public class CommandSettings : SettingsConfigurationSection, ICommandSettings
	{
		public string Name { get; protected set; }

		/// <summary>
		/// Тип
		/// </summary>
		public string Type { get; protected set; }

		/// <summary>
		/// Тип
		/// </summary>
		public TimeSpan Period { get; protected set; }

		public TimeSpan DueTime { get; protected set; }

		public RepeaterConcurrentMode Mode { get; protected set; }

		public CommandSettings(string name, IConfigurationSection raw)
			: base(raw)
		{
			Name = name;
		}

		protected override void LoadSettings(IConfigurationSection raw)
		{
			Name = GetAttValue(raw, "name");
			Type = GetAttValue(raw, "type");
			var periodStr = GetAttValue(raw, "period");
			Period = TimeSpan.Parse(periodStr);
			Mode = GetAttBoolValue(raw, "isConcurrentMode", true)? RepeaterConcurrentMode.AllowConcurrenceMode :  RepeaterConcurrentMode.DisallowConcurrentMode;

		}
	}
}
