using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Commands.Scheduling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bsa.Msa.RabbitMq.Core.Tests.Repeaters
{
	/// <summary>
	/// Tests of the repeaters and <see cref="RepeatingCommandContainer"/>.
	/// </summary>
	[TestClass]
	public sealed class RepeaterTests
	{
		private static readonly TimeSpan Period = TimeSpan.FromMilliseconds(30);

		private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

		[TestMethod]
		public void DisallowConcurrentMode_RunsDoNotOverlap()
		{
			var settings = CreateSettings(RepeaterConcurrentMode.DisallowConcurrentMode);
			var stats = new RunStats();

			using (var repeater = new DisallowConcurrentModeRepeater(settings))
			{
				repeater.Start(token => stats.Run(TimeSpan.FromMilliseconds(100)));
				Assert.IsTrue(SpinWait.SpinUntil(() => stats.Runs >= 3, WaitTimeout));
			}

			Assert.AreEqual(1, stats.MaxConcurrent);
		}

		[TestMethod]
		public void AllowConcurrenceMode_RunsOverlap()
		{
			var settings = CreateSettings(RepeaterConcurrentMode.AllowConcurrenceMode);
			var stats = new RunStats();

			using (var repeater = new AllowConcurrenceModeRepeater(settings))
			{
				repeater.Start(token => stats.Run(TimeSpan.FromMilliseconds(200)));
				Assert.IsTrue(SpinWait.SpinUntil(() => stats.MaxConcurrent > 1, WaitTimeout));
			}
		}

		[TestMethod]
		public void Dispose_StopsRuns()
		{
			var settings = CreateSettings(RepeaterConcurrentMode.DisallowConcurrentMode);
			var stats = new RunStats();
			var repeater = new DisallowConcurrentModeRepeater(settings);

			repeater.Start(token => stats.Run(TimeSpan.Zero));
			Assert.IsTrue(SpinWait.SpinUntil(() => stats.Runs >= 1, WaitTimeout));
			repeater.Dispose();
			Thread.Sleep(100);
			var runs = stats.Runs;
			Thread.Sleep(200);

			Assert.AreEqual(runs, stats.Runs);
		}

		[TestMethod]
		public void Container_DisposesEachCommandAndCancelsTokenOnStop()
		{
			var settings = CreateSettings(RepeaterConcurrentMode.DisallowConcurrentMode);
			var commands = new ConcurrentQueue<TokenCommand>();
			var factory = new Mock<ICommandFactory>();
			factory
				.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<ISettings>(), It.IsAny<CancellationToken>()))
				.Returns<string, ISettings, CancellationToken>((type, s, token) =>
				{
					var command = new TokenCommand(s, token);
					commands.Enqueue(command);
					return command;
				});
			var container = new RepeatingCommandContainer(settings, new RepeaterFactory(), factory.Object);

			container.Start();
			Assert.IsTrue(SpinWait.SpinUntil(() => commands.Count(x => x.IsDisposed) >= 2, WaitTimeout));
			container.Stop();
			Thread.Sleep(100);

			Assert.IsTrue(commands.All(x => x.IsDisposed));
			Assert.IsTrue(commands.All(x => x.ExecuteCount == 1));
			Assert.IsTrue(commands.Last().Token.IsCancellationRequested);
		}

		private static FakeCommandSettings CreateSettings(RepeaterConcurrentMode mode)
		{
			return new FakeCommandSettings(new IntervalSchedule(Period, TimeSpan.Zero), mode);
		}

		private sealed class RunStats
		{
			private int _current;

			private int _runs;

			private int _maxConcurrent;

			public int Runs => Volatile.Read(ref _runs);

			public int MaxConcurrent => Volatile.Read(ref _maxConcurrent);

			public void Run(TimeSpan duration)
			{
				var current = Interlocked.Increment(ref _current);
				int max;
				while (current > (max = Volatile.Read(ref _maxConcurrent)))
					Interlocked.CompareExchange(ref _maxConcurrent, current, max);

				Thread.Sleep(duration);
				Interlocked.Decrement(ref _current);
				Interlocked.Increment(ref _runs);
			}
		}
	}
}
