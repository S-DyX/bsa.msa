using System;
using System.Collections.Generic;
using Bsa.Msa.Common.Services.Commands.Scheduling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Scheduling
{
	/// <summary>
	/// Helpers for the schedule tests.
	/// </summary>
	internal static class ScheduleAssert
	{
		/// <summary>
		/// Returns up to <paramref name="count"/> next runs.
		/// </summary>
		public static List<DateTime> TakeRuns(
			ICommandSchedule schedule,
			DateTime from,
			int count)
		{
			var result = new List<DateTime>();
			var notBefore = from;
			for (var i = 0; i < count; i++)
			{
				var run = schedule.GetNextRun(notBefore);
				if (!run.HasValue) break;

				result.Add(run.Value);
				notBefore = run.Value.AddTicks(1);
			}

			return result;
		}

		/// <summary>
		/// Checks the next runs of the schedule.
		/// </summary>
		public static void Runs(
			ICommandSchedule schedule,
			DateTime from,
			params DateTime[] expected)
		{
			var actual = TakeRuns(schedule, from, expected.Length);
			CollectionAssert.AreEqual(expected, actual);
		}
	}
}
