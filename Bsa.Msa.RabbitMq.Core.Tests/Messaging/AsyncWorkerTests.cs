using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Messaging
{
	/// <summary>
	/// Tests of <see cref="AsyncWorker"/>.
	/// </summary>
	[TestClass]
	public sealed class AsyncWorkerTests
	{
		[TestMethod]
		public void Worker_ProcessesQueuedActionsAndSurvivesErrors()
		{
			var queue = new ConcurrentQueue<Action>();
			using var done = new ManualResetEventSlim();
			queue.Enqueue(() => throw new InvalidOperationException("boom"));
			queue.Enqueue(done.Set);
			using var worker = new AsyncWorker(null, queue, x => { }, "test");

			Assert.IsTrue(done.Wait(TimeSpan.FromSeconds(5)));
			worker.Stop();
		}
	}
}
