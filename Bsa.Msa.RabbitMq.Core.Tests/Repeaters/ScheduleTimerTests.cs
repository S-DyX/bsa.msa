using System;
using System.Threading;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Repeaters
{
	/// <summary>
	/// Tests of <see cref="ScheduleTimer"/>.
	/// </summary>
	[TestClass]
	public sealed class ScheduleTimerTests
	{
		private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

		[TestMethod]
		public void Start_RunsAtScheduledTime()
		{
			var runAt = DateTime.Now.AddMilliseconds(100);
			var schedule = new DelegateSchedule(x => x <= runAt ? runAt : (DateTime?)null);
			using var fired = new ManualResetEventSlim();
			using var timer = new ScheduleTimer(schedule, fired.Set, null, "test");

			timer.Start();

			Assert.IsTrue(fired.Wait(WaitTimeout));
			Assert.IsTrue(DateTime.Now >= runAt);
			Assert.IsNull(timer.NextRun);
		}

		[TestMethod]
		public void Dispose_BeforeDue_DoesNotRun()
		{
			var runAt = DateTime.Now.AddMilliseconds(200);
			var schedule = new DelegateSchedule(x => runAt);
			using var fired = new ManualResetEventSlim();
			var timer = new ScheduleTimer(schedule, fired.Set, null, "test");

			timer.Start();
			timer.Dispose();

			Assert.IsFalse(fired.Wait(TimeSpan.FromMilliseconds(500)));
		}

		[TestMethod]
		public void Start_NoRuns_NextRunIsNull()
		{
			var schedule = new DelegateSchedule(x => null);
			using var timer = new ScheduleTimer(schedule, () => Assert.Fail("Must not run."), null, "test");

			timer.Start();

			Assert.IsNull(timer.NextRun);
		}

		[TestMethod]
		public void Run_PlansNextRunBeforeExecution()
		{
			var count = 0;
			var schedule = new DelegateSchedule(x => x);
			using var done = new ManualResetEventSlim();
			ScheduleTimer timer = null;
			timer = new ScheduleTimer(
				schedule,
				() =>
				{
					// The next run is already planned while this one runs.
					if (timer.NextRun.HasValue && Interlocked.Increment(ref count) == 3)
						done.Set();
				},
				null,
				"test");

			timer.Start();

			Assert.IsTrue(done.Wait(WaitTimeout));
			timer.Dispose();
		}
	}
}
