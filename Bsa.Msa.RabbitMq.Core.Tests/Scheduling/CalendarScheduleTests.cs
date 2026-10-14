using System;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Commands.Scheduling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Scheduling
{
	/// <summary>
	/// Tests of <see cref="DailySchedule"/>, <see cref="MonthlySchedule"/>
	/// and <see cref="BoundedSchedule"/>. 2026-10-14 is a Wednesday.
	/// </summary>
	[TestClass]
	public sealed class CalendarScheduleTests
	{
		private static readonly DateTime Noon = new DateTime(2026, 10, 14, 12, 30, 0);

		private static readonly TimeSpan BeforeMidnight = new TimeSpan(23, 59, 0);

		[TestMethod]
		public void Daily_RunsEveryDayAtTime()
		{
			var schedule = new DailySchedule(new[] { BeforeMidnight });

			ScheduleAssert.Runs(
				schedule,
				Noon,
				new DateTime(2026, 10, 14, 23, 59, 0),
				new DateTime(2026, 10, 15, 23, 59, 0));
		}

		[TestMethod]
		public void Daily_RunTimeEqualsNotBefore_ReturnsIt()
		{
			var schedule = new DailySchedule(new[] { BeforeMidnight });
			var run = new DateTime(2026, 10, 14, 23, 59, 0);

			Assert.AreEqual(run, schedule.GetNextRun(run));
		}

		[TestMethod]
		public void Weekly_UnsortedTimes_RunsOnGivenDaysInOrder()
		{
			var schedule = new DailySchedule(
				new[] { TimeSpan.FromHours(14), TimeSpan.FromHours(2) },
				DaysOfWeek.Monday | DaysOfWeek.Thursday);

			ScheduleAssert.Runs(
				schedule,
				Noon,
				new DateTime(2026, 10, 15, 2, 0, 0),
				new DateTime(2026, 10, 15, 14, 0, 0),
				new DateTime(2026, 10, 19, 2, 0, 0));
		}

		[TestMethod]
		public void Monthly_DaysFromStartAndEnd()
		{
			var schedule = new MonthlySchedule(new[] { 1, 15, -1 }, new[] { BeforeMidnight });

			ScheduleAssert.Runs(
				schedule,
				Noon,
				new DateTime(2026, 10, 15, 23, 59, 0),
				new DateTime(2026, 10, 31, 23, 59, 0),
				new DateTime(2026, 11, 1, 23, 59, 0),
				new DateTime(2026, 11, 15, 23, 59, 0),
				new DateTime(2026, 11, 30, 23, 59, 0));
		}

		[TestMethod]
		public void Monthly_MissingDay_SkipsShortMonths()
		{
			var schedule = new MonthlySchedule(new[] { 31 }, new[] { TimeSpan.FromHours(1) });

			ScheduleAssert.Runs(
				schedule,
				new DateTime(2027, 1, 31, 2, 0, 0),
				new DateTime(2027, 3, 31, 1, 0, 0),
				new DateTime(2027, 5, 31, 1, 0, 0));
		}

		[TestMethod]
		public void Monthly_LastDay_UsesLeapFebruary()
		{
			var schedule = new MonthlySchedule(new[] { -1 }, new[] { TimeSpan.Zero });

			var run = schedule.GetNextRun(new DateTime(2028, 2, 2));

			Assert.AreEqual(new DateTime(2028, 2, 29), run);
		}

		[TestMethod]
		public void Bounded_StartDate_NoRunsBefore()
		{
			var inner = new DailySchedule(new[] { TimeSpan.FromHours(10) });
			var schedule = new BoundedSchedule(inner, new DateTime(2026, 11, 1), null);

			Assert.AreEqual(
				new DateTime(2026, 11, 1, 10, 0, 0),
				schedule.GetNextRun(Noon));
		}

		[TestMethod]
		public void Bounded_EndDate_StopsRuns()
		{
			var inner = new DailySchedule(new[] { TimeSpan.FromHours(10) });
			var schedule = new BoundedSchedule(inner, null, new DateTime(2026, 10, 16));

			ScheduleAssert.Runs(
				schedule,
				Noon,
				new DateTime(2026, 10, 15, 10, 0, 0));
			Assert.IsNull(schedule.GetNextRun(new DateTime(2026, 10, 15, 11, 0, 0)));
		}

		[TestMethod]
		public void Guard_NoTimes_Throws()
		{
			Assert.ThrowsException<ArgumentException>(
				() => new DailySchedule(Array.Empty<TimeSpan>()));
		}

		[TestMethod]
		public void Guard_TimeOutsideDay_Throws()
		{
			Assert.ThrowsException<ArgumentOutOfRangeException>(
				() => new DailySchedule(new[] { TimeSpan.FromHours(24) }));
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(32)]
		[DataRow(-32)]
		public void Guard_InvalidDayOfMonth_Throws(int day)
		{
			Assert.ThrowsException<ArgumentOutOfRangeException>(
				() => new MonthlySchedule(new[] { day }, new[] { TimeSpan.Zero }));
		}

		[TestMethod]
		[DataRow(DaysOfWeek.Sunday, true)]
		[DataRow(DaysOfWeek.Weekends, true)]
		[DataRow(DaysOfWeek.WorkingDays, false)]
		[DataRow(DaysOfWeek.None, true)]
		public void DaysOfWeekHelper_Sunday(DaysOfWeek days, bool expected)
		{
			var sunday = new DateTime(2026, 10, 18);

			Assert.AreEqual(expected, DaysOfWeekHelper.IsAllowed(days, sunday));
		}
	}
}
