using System;
using System.Collections.Generic;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Commands.Scheduling;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Scheduling
{
	/// <summary>
	/// Tests of <see cref="CommandScheduleParser"/> and <see cref="CommandSettings"/>.
	/// </summary>
	[TestClass]
	public sealed class CommandScheduleParserTests
	{
		private static readonly DateTime Noon = new DateTime(2026, 10, 14, 12, 30, 0);

		[TestMethod]
		public void Parse_Daily_UsesAt()
		{
			var schedule = Parse(("schedule", "daily"), ("at", "23:59"));

			Assert.IsInstanceOfType(schedule, typeof(DailySchedule));
			Assert.AreEqual(new DateTime(2026, 10, 14, 23, 59, 0), schedule.GetNextRun(Noon));
		}

		[TestMethod]
		public void Parse_Daily_StartIsAliasOfAt()
		{
			var schedule = Parse(("schedule", "Daily"), ("start", "23:59"));

			Assert.AreEqual(new DateTime(2026, 10, 14, 23, 59, 0), schedule.GetNextRun(Noon));
		}

		[TestMethod]
		public void Parse_Weekly_DaysSeparatedBySemicolon()
		{
			var schedule = Parse(
				("schedule", "weekly"),
				("days", "Monday;Thursday"),
				("at", "14:00;02:00"));

			ScheduleAssert.Runs(
				schedule,
				Noon,
				new DateTime(2026, 10, 15, 2, 0, 0),
				new DateTime(2026, 10, 15, 14, 0, 0),
				new DateTime(2026, 10, 19, 2, 0, 0));
		}

		[TestMethod]
		public void Parse_Monthly_DaysOfMonth()
		{
			var schedule = Parse(
				("schedule", "monthly"),
				("daysOfMonth", "1;-1"),
				("at", "23:59"));

			Assert.AreEqual(new DateTime(2026, 10, 31, 23, 59, 0), schedule.GetNextRun(Noon));
		}

		[TestMethod]
		public void Parse_IntervalWindow()
		{
			var schedule = Parse(
				("period", "00:10:00"),
				("from", "23:59"),
				("to", "06:00"),
				("days", "WorkingDays"));

			Assert.IsInstanceOfType(schedule, typeof(IntervalSchedule));
			Assert.AreEqual(new DateTime(2026, 10, 14, 23, 59, 0), schedule.GetNextRun(Noon));
		}

		[TestMethod]
		public void Parse_Dates_WrapsIntoBoundedSchedule()
		{
			var schedule = Parse(
				("schedule", "daily"),
				("at", "10:00"),
				("startDate", "2026-11-01"),
				("endDate", "2026-12-01"));

			Assert.IsInstanceOfType(schedule, typeof(BoundedSchedule));
			Assert.AreEqual(new DateTime(2026, 11, 1, 10, 0, 0), schedule.GetNextRun(Noon));
		}

		[TestMethod]
		public void Parse_WeeklyWithoutDays_Throws()
		{
			Assert.ThrowsException<InvalidOperationException>(
				() => Parse(("schedule", "weekly"), ("at", "10:00")));
		}

		[TestMethod]
		public void Parse_IntervalWithoutPeriod_Throws()
		{
			Assert.ThrowsException<InvalidOperationException>(
				() => Parse(("start", "10:00")));
		}

		[TestMethod]
		public void Parse_UnknownKind_ThrowsWithCommandName()
		{
			var ex = Assert.ThrowsException<InvalidOperationException>(
				() => Parse(("schedule", "yearly"), ("at", "10:00")));

			StringAssert.Contains(ex.Message, "Nightly");
		}

		[TestMethod]
		public void ParseDays_Empty_ReturnsNull()
		{
			Assert.IsNull(CommandScheduleParser.ParseDays(" "));
		}

		[TestMethod]
		public void CommandSettings_LegacyKeys_BuildIntervalSchedule()
		{
			var section = ConfigurationFactory.CreateSection(new Dictionary<string, string>
			{
				["name"] = "Legacy",
				["type"] = "TokenCommand",
				["period"] = "00:05:00",
				["start"] = "14:00:00",
				["days"] = "Monday, Friday",
				["isConcurrentMode"] = "false"
			});

			var settings = new CommandSettings("Legacy", section);

			Assert.AreEqual(TimeSpan.FromMinutes(5), settings.Period);
			Assert.AreEqual(TimeSpan.FromHours(14), settings.DueTime);
			Assert.AreEqual(DaysOfWeek.Monday | DaysOfWeek.Friday, settings.DaysOfWeek);
			Assert.AreEqual(RepeaterConcurrentMode.DisallowConcurrentMode, settings.Mode);
			Assert.IsInstanceOfType(settings.Schedule, typeof(IntervalSchedule));
		}

		[TestMethod]
		public void CommandSettings_DailyWithoutPeriod_IsValid()
		{
			var section = ConfigurationFactory.CreateSection(new Dictionary<string, string>
			{
				["name"] = "Nightly",
				["type"] = "TokenCommand",
				["schedule"] = "daily",
				["at"] = "23:59"
			});

			var settings = new CommandSettings("Nightly", section);

			Assert.AreEqual(RepeaterConcurrentMode.AllowConcurrenceMode, settings.Mode);
			Assert.IsInstanceOfType(settings.Schedule, typeof(DailySchedule));
		}

		private static ICommandSchedule Parse(params (string Key, string Value)[] values)
		{
			var map = new Dictionary<string, string>();
			foreach (var (key, value) in values)
				map[key] = value;

			return CommandScheduleParser.Parse(
				key => map.TryGetValue(key, out var value) ? value : null,
				"Nightly");
		}
	}
}
