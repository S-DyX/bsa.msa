using System;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Commands.Scheduling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Scheduling
{
	/// <summary>
	/// Tests of <see cref="IntervalSchedule"/>. 2026-10-14 is a Wednesday.
	/// </summary>
	[TestClass]
	public sealed class IntervalScheduleTests
	{
		private static readonly DateTime Noon = new DateTime(2026, 10, 14, 12, 30, 0);

		[TestMethod]
		public void GetNextRun_WithoutStart_RunsImmediatelyThenEveryPeriod()
		{
			var schedule = new IntervalSchedule(TimeSpan.FromMinutes(5), TimeSpan.Zero);

			ScheduleAssert.Runs(
				schedule,
				Noon,
				Noon,
				Noon.AddMinutes(5),
				Noon.AddMinutes(10));
		}

		[TestMethod]
		public void GetNextRun_StartLaterToday_FirstRunAtStart()
		{
			var schedule = new IntervalSchedule(TimeSpan.FromHours(1), TimeSpan.FromHours(14));

			ScheduleAssert.Runs(
				schedule,
				Noon,
				new DateTime(2026, 10, 14, 14, 0, 0),
				new DateTime(2026, 10, 14, 15, 0, 0));
		}

		[TestMethod]
		public void GetNextRun_StartAlreadyPassed_FirstRunTomorrow()
		{
			var schedule = new IntervalSchedule(TimeSpan.FromHours(1), TimeSpan.FromHours(10));

			ScheduleAssert.Runs(
				schedule,
				Noon,
				new DateTime(2026, 10, 15, 10, 0, 0),
				new DateTime(2026, 10, 15, 11, 0, 0));
		}

		[TestMethod]
		public void GetNextRun_WindowAcrossMidnight_RunsOnlyInsideWindow()
		{
			var schedule = new IntervalSchedule(
				TimeSpan.FromMinutes(30),
				TimeSpan.Zero,
				new TimeSpan(23, 59, 0),
				TimeSpan.FromHours(2));

			ScheduleAssert.Runs(
				schedule,
				Noon,
				new DateTime(2026, 10, 14, 23, 59, 0),
				new DateTime(2026, 10, 15, 0, 29, 0),
				new DateTime(2026, 10, 15, 0, 59, 0),
				new DateTime(2026, 10, 15, 1, 29, 0),
				new DateTime(2026, 10, 15, 1, 59, 0),
				new DateTime(2026, 10, 15, 23, 59, 0));
		}

		[TestMethod]
		public void GetNextRun_InsideWindow_ContinuesWindowGrid()
		{
			var schedule = new IntervalSchedule(
				TimeSpan.FromMinutes(30),
				TimeSpan.Zero,
				new TimeSpan(23, 59, 0),
				TimeSpan.FromHours(2));

			var run = schedule.GetNextRun(new DateTime(2026, 10, 15, 0, 40, 0));

			Assert.AreEqual(new DateTime(2026, 10, 15, 0, 59, 0), run);
		}

		[TestMethod]
		public void GetNextRun_WorkingDays_SkipsWeekend()
		{
			var schedule = new IntervalSchedule(
				TimeSpan.FromHours(6),
				TimeSpan.Zero,
				days: DaysOfWeek.WorkingDays);
			var friday = new DateTime(2026, 10, 16, 12, 0, 0);

			ScheduleAssert.Runs(
				schedule,
				friday,
				friday,
				friday.AddHours(6),
				new DateTime(2026, 10, 19, 0, 0, 0),
				new DateTime(2026, 10, 19, 6, 0, 0));
		}

		[TestMethod]
		public void GetNextRun_WindowDays_CheckDayWindowStartsOn()
		{
			// The Friday window lasts till Saturday 02:00.
			var schedule = new IntervalSchedule(
				TimeSpan.FromHours(1),
				TimeSpan.Zero,
				TimeSpan.FromHours(23),
				TimeSpan.FromHours(2),
				DaysOfWeek.Friday);

			ScheduleAssert.Runs(
				schedule,
				new DateTime(2026, 10, 16, 22, 0, 0),
				new DateTime(2026, 10, 16, 23, 0, 0),
				new DateTime(2026, 10, 17, 0, 0, 0),
				new DateTime(2026, 10, 17, 1, 0, 0),
				new DateTime(2026, 10, 23, 23, 0, 0));
		}

		[TestMethod]
		public void Ctor_NotPositivePeriod_Throws()
		{
			Assert.ThrowsException<ArgumentOutOfRangeException>(
				() => new IntervalSchedule(TimeSpan.Zero, TimeSpan.Zero));
		}
	}
}
