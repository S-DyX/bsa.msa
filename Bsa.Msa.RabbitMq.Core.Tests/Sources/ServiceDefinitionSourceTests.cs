using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bsa.Msa.Common.Services.Commands.Scheduling;
using Bsa.Msa.Common.Services.Settings;
using Bsa.Msa.Common.Services.Sources;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bsa.Msa.RabbitMq.Core.Tests.Sources
{
	/// <summary>
	/// Tests of the definition sources and <see cref="ServiceDefinitionFactory"/>.
	/// </summary>
	[TestClass]
	public sealed class ServiceDefinitionSourceTests
	{
		[TestMethod]
		public void Factory_CreateHandler_FromValues()
		{
			var handler = ServiceDefinitionFactory.CreateHandler(new Dictionary<string, string>
			{
				["name"] = "orders",
				["type"] = "SyncTestHandler",
				["subscriptionEndpoint"] = "orders.queue",
				["degreeOfParallelism"] = "4",
				["retry"] = "true"
			});

			Assert.AreEqual("orders", handler.Name);
			Assert.AreEqual("orders.queue", handler.SubscriptionEndpoint);
			Assert.AreEqual(4, handler.DegreeOfParallelism);
			Assert.IsTrue(handler.Retry);
		}

		[TestMethod]
		public void Factory_CreateCommand_FromValues()
		{
			var command = ServiceDefinitionFactory.CreateCommand(new Dictionary<string, string>
			{
				["name"] = "nightly",
				["type"] = "TokenCommand",
				["schedule"] = "daily",
				["at"] = "23:59"
			});

			Assert.AreEqual("nightly", command.Name);
			Assert.IsInstanceOfType(command.Schedule, typeof(DailySchedule));
		}

		[TestMethod]
		public void Factory_WithoutName_Throws()
		{
			Assert.ThrowsException<ArgumentException>(
				() => ServiceDefinitionFactory.CreateHandler(new Dictionary<string, string> { ["type"] = "X" }));
		}

		[TestMethod]
		public void Factory_FromJson_ReadsServicesFormat()
		{
			const string json = @"{
				""services"": [
					{
						""name"": ""service"",
						// comments are allowed, as in services.json
						""handlers"": [ { ""name"": ""h1"", ""type"": ""SyncTestHandler"", ""subscriptionEndpoint"": ""q1"" } ],
						""commands"": [ { ""name"": ""c1"", ""type"": ""TokenCommand"", ""period"": ""00:01:00"" } ]
					}
				]
			}";

			var definitions = ServiceDefinitionFactory.FromJson(json);

			Assert.AreEqual("h1", definitions.Handlers.Single().Name);
			Assert.AreEqual("c1", definitions.Commands.Single().Name);
		}

		[TestMethod]
		public void Definitions_Fingerprint_ChangesWithSettings()
		{
			var first = CreateDefinitions("q1");
			var same = CreateDefinitions("q1");
			var changed = CreateDefinitions("q2");

			Assert.AreEqual(first.GetFingerprint(), same.GetFingerprint());
			Assert.AreNotEqual(first.GetFingerprint(), changed.GetFingerprint());
		}

		[TestMethod]
		public void ServicesJsonSource_ReturnsHandlersOfAllServices()
		{
			var service = new ServiceSettings("service", ConfigurationFactory.CreateSection(
				new Dictionary<string, string>
				{
					["name"] = "service",
					["handlers:0:name"] = "h1",
					["handlers:0:type"] = "SyncTestHandler",
					["handlers:0:subscriptionEndpoint"] = "q1"
				}));
			var settings = new Mock<IServicesSettings>();
			settings.Setup(x => x.GetServices()).Returns(new[] { service });

			var definitions = new ServicesJsonDefinitionSource(settings.Object).Load();

			Assert.AreEqual("q1", definitions.Handlers.Single().SubscriptionEndpoint);
		}

		[TestMethod]
		public void PollingSource_RaisesChangedOnlyOnChange()
		{
			using var source = new TestPollingSource(CreateDefinitions("q1"));

			// Polls before Load have nobody to notify; let them settle.
			Thread.Sleep(100);
			var changes = 0;
			source.Changed += (sender, e) => Interlocked.Increment(ref changes);
			source.Load();

			Thread.Sleep(200);
			Assert.AreEqual(0, Volatile.Read(ref changes));

			source.Current = CreateDefinitions("q2");

			Assert.IsTrue(SpinWait.SpinUntil(() => Volatile.Read(ref changes) == 1, TimeSpan.FromSeconds(5)));
			Thread.Sleep(200);
			Assert.AreEqual(1, Volatile.Read(ref changes));
		}

		internal static ServiceDefinitions CreateDefinitions(params string[] endpoints)
		{
			var handlers = endpoints.Select(x => ServiceDefinitionFactory.CreateHandler(
				new Dictionary<string, string>
				{
					["name"] = x,
					["type"] = "SyncTestHandler",
					["subscriptionEndpoint"] = x
				}));
			return new ServiceDefinitions(handlers, null);
		}

		private sealed class TestPollingSource : PollingServiceDefinitionSource
		{
			public TestPollingSource(ServiceDefinitions current)
				: base(TimeSpan.FromMilliseconds(30))
			{
				Current = current;
			}

			public ServiceDefinitions Current { get; set; }

			protected override ServiceDefinitions LoadDefinitions()
			{
				return Current;
			}
		}
	}
}
