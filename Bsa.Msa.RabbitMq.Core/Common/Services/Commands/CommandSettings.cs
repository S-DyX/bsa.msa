using System;
using System.Globalization;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands.Scheduling;
using Bsa.Msa.Common.Settings;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.Common.Services.Commands
{

	public interface ICommandSettings : ISettings
	{
		string Name { get; }

		/// <summary>
		/// ICommand type
		/// </summary>
		string Type { get; }

		/// <summary>
		/// Period
		/// </summary>
		TimeSpan Period { get; }

		TimeSpan DueTime { get; }

		/// <summary>
		/// <see cref="RepeaterConcurrentMode"/>
		/// </summary>
		RepeaterConcurrentMode Mode { get; }

		/// <summary>
		/// <see cref="DaysOfWeek"/>
		/// </summary>
		DaysOfWeek? DaysOfWeek { get; }

		/// <summary>
		/// Schedule of the command runs. By default it is built
		/// from <see cref="Period"/>, <see cref="DueTime"/>
		/// and <see cref="DaysOfWeek"/>.
		/// </summary>
		ICommandSchedule Schedule => new IntervalSchedule(
			Period,
			DueTime,
			days: DaysOfWeek);
	}
	[Flags]
	public enum DaysOfWeek
	{
		None = 0,

		/// <summary>
		///  Indicates Monday.
		/// </summary>
		Monday = 1,

		/// <summary>
		/// Indicates Tuesday.
		/// </summary>
		Tuesday = 2,

		/// <summary>
		/// Indicates Wednesday.
		/// </summary>
		Wednesday = 4,

		/// <summary>
		/// Indicates Thursday.
		/// </summary>
		Thursday = 8,

		/// <summary>
		/// Indicates Friday.
		/// </summary>
		Friday = 16,

		/// <summary>
		/// Indicates Saturday.
		/// </summary>
		Saturday = 32,

		/// <summary>
		/// Sunday
		/// </summary>
		Sunday = 64,

		/// <summary>
		/// Weekends
		/// </summary>
		Weekends = Saturday | Sunday,

		/// <summary>
		/// WorkingDays
		/// </summary>
		WorkingDays = Monday | Tuesday | Wednesday | Thursday | Friday,

		All = WorkingDays | Weekends,
	}
	/// <summary>
	/// Logical operations
	/// </summary>
	public class CommandSettings : SettingsConfigurationSection, ICommandSettings
	{
		public string Name { get; protected set; }

		/// <summary>
		/// Type
		/// </summary>
		public string Type { get; protected set; }

		/// <summary>
		/// Period
		/// </summary>
		public TimeSpan Period { get; protected set; }

		public TimeSpan DueTime { get; protected set; } 


		public DaysOfWeek? DaysOfWeek { get; protected set; }
		/// <summary>
		/// <see cref="RepeaterConcurrentMode"/>
		/// </summary>
		public RepeaterConcurrentMode Mode { get; protected set; }

		/// <inheritdoc/>
		public ICommandSchedule Schedule { get; protected set; }

		public CommandSettings(string name, IConfigurationSection raw)
			: base(raw)
		{
			Name = name;
		}

		protected override void LoadSettings(IConfigurationSection raw)
		{
			Name = GetAttValue(raw, "name");
			Type = GetAttValue(raw, "type");
			// "period" is required only by the interval schedule.
			var periodStr = GetAttValue(raw, "period");
			if (!string.IsNullOrEmpty(periodStr))
				Period = TimeSpan.Parse(periodStr, CultureInfo.InvariantCulture);

			var startStr = GetAttValue(raw, "start");
			if (!string.IsNullOrEmpty(startStr))
				DueTime = TimeSpan.Parse(startStr, CultureInfo.InvariantCulture);

			DaysOfWeek = CommandScheduleParser.ParseDays(GetAttValue(raw, "days"));

			Mode = GetAttBoolValue(raw, "isConcurrentMode", true) ? RepeaterConcurrentMode.AllowConcurrenceMode : RepeaterConcurrentMode.DisallowConcurrentMode;

			Schedule = CommandScheduleParser.Parse(
				key => GetAttValue(raw, key),
				Name);
		}
	}
}
