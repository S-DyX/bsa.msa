using System;
using System.Collections.Generic;
using System.Linq;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Impl;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Services.Settings;
using Bsa.Msa.Common.Services.Sources;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bsa.Msa.RabbitMq.Core.Tests.Sources
{
	/// <summary>
	/// Tests of <see cref="ServiceUnitManager"/>: loading from a source,
	/// reloading and runtime control.
	/// </summary>
	[TestClass]
	public sealed class ServiceUnitManagerTests
	{
		private FakeDefinitionSource _source;

		private Mock<ISubscriberFactory> _subscriberFactory;

		private List<CreatedSubscriber> _created;

		private ServiceUnitManager _manager;

		[TestInitialize]
		public void Initialize()
		{
			_source = new FakeDefinitionSource();
			_created = new List<CreatedSubscriber>();
			_subscriberFactory = new Mock<ISubscriberFactory>();
			_subscriberFactory
				.Setup(x => x.Create(It.IsAny<IMessageHandlerSettings>(), It.IsAny<IMessageHandlerFactory>()))
				.Returns<IMessageHandlerSettings, IMessageHandlerFactory>((settings, factory) =>
				{
					var subscriber = new Mock<ISubscriber>();
					subscriber.Setup(x => x.Name).Returns(settings.SubscriptionEndpoint);
					_created.Add(new CreatedSubscriber(settings.SubscriptionEndpoint, subscriber));
					return subscriber.Object;
				});
			_manager = CreateManager(null, _source);
		}

		[TestCleanup]
		public void Cleanup()
		{
			_manager.Stop();
		}

		[TestMethod]
		public void Start_StartsHandlersFromSource()
		{
			_source.Definitions = ServiceDefinitionSourceTests.CreateDefinitions("q1", "q2");

			_manager.Start();

			CollectionAssert.AreEquivalent(new[] { "q1", "q2" }, _created.Select(x => x.Endpoint).ToList());
			foreach (var created in _created)
				created.Mock.Verify(x => x.Start(), Times.Once);
		}

		[TestMethod]
		public void Start_WithoutSource_UsesServicesJsonSettings()
		{
			var service = new ServiceSettings("service", ConfigurationFactory.CreateSection(
				new Dictionary<string, string>
				{
					["name"] = "service",
					["handlers:0:name"] = "h1",
					["handlers:0:type"] = "SyncTestHandler",
					["handlers:0:subscriptionEndpoint"] = "json.queue"
				}));
			var servicesSettings = new Mock<IServicesSettings>();
			servicesSettings.Setup(x => x.GetServices()).Returns(new[] { service });
			var manager = CreateManager(servicesSettings.Object, null);

			manager.Start();
			manager.Stop();

			Assert.AreEqual("json.queue", _created.Single().Endpoint);
		}

		[TestMethod]
		public void Start_ZeroThreads_DeletesQueueInsteadOfStarting()
		{
			_source.Definitions = new ServiceDefinitions(new[] { CreateHandler("q1", "0") }, null);

			_manager.Start();

			Assert.AreEqual(0, _created.Count);
			_subscriberFactory.Verify(x => x.Delete(It.IsAny<IMessageHandlerSettings>()), Times.Once);
		}

		[TestMethod]
		public void StopHandler_StopsAndStartHandler_CreatesNewSubscriber()
		{
			_source.Definitions = ServiceDefinitionSourceTests.CreateDefinitions("q1");
			_manager.Start();

			Assert.IsTrue(_manager.StopHandler("q1"));

			_created[0].Mock.Verify(x => x.Stop(), Times.Once);
			Assert.IsFalse(_manager.GetUnits().Single().IsEnabled);

			Assert.IsTrue(_manager.StartHandler("q1"));

			Assert.AreEqual(2, _created.Count);
			_created[1].Mock.Verify(x => x.StartAsync(), Times.Once);
			Assert.IsTrue(_manager.GetUnits().Single().IsEnabled);
		}

		[TestMethod]
		public void StopHandler_UnknownName_ReturnsFalse()
		{
			_manager.Start();

			Assert.IsFalse(_manager.StopHandler("missing"));
		}

		[TestMethod]
		public void SourceChanged_AddsRemovesAndRestartsChangedHandlers()
		{
			_source.Definitions = new ServiceDefinitions(
				new[] { CreateHandler("keep"), CreateHandler("remove"), CreateHandler("change") },
				null);
			_manager.Start();
			var change = _created.Single(x => x.Endpoint == "change");

			_source.Publish(new ServiceDefinitions(
				new[]
				{
					CreateHandler("keep"),
					CreateHandler("change", "2"),
					CreateHandler("new")
				},
				null));

			Get("remove").Mock.Verify(x => x.Stop(), Times.Once);
			change.Mock.Verify(x => x.Stop(), Times.Once);
			Get("keep").Mock.Verify(x => x.Stop(), Times.Never);
			Assert.AreEqual(1, _created.Count(x => x.Endpoint == "keep"));
			Assert.AreEqual(2, _created.Count(x => x.Endpoint == "change"));
			Get("new").Mock.Verify(x => x.StartAsync(), Times.Once);
		}

		[TestMethod]
		public void SourceChanged_StoppedHandlerStaysStopped()
		{
			_source.Definitions = ServiceDefinitionSourceTests.CreateDefinitions("q1");
			_manager.Start();
			_manager.StopHandler("q1");

			_source.Publish(new ServiceDefinitions(new[] { CreateHandler("q1", "3") }, null));

			Assert.AreEqual(1, _created.Count);
			Assert.IsFalse(_manager.GetUnits().Single().IsEnabled);
		}

		[TestMethod]
		public void AddHandler_RuntimeHandlerSurvivesReload()
		{
			_manager.Start();

			_manager.AddHandler(CreateHandler("runtime"));
			_source.Publish(ServiceDefinitions.Empty);

			var created = Get("runtime");
			created.Mock.Verify(x => x.StartAsync(), Times.Once);
			created.Mock.Verify(x => x.Stop(), Times.Never);
			Assert.AreEqual("runtime", _manager.GetUnits().Single().Name);
		}

		[TestMethod]
		public void AddHandler_BeforeStart_Throws()
		{
			Assert.ThrowsException<InvalidOperationException>(
				() => _manager.AddHandler(CreateHandler("q1")));
		}

		[TestMethod]
		public void RemoveHandler_StopsAndForgets()
		{
			_source.Definitions = ServiceDefinitionSourceTests.CreateDefinitions("q1");
			_manager.Start();

			Assert.IsTrue(_manager.RemoveHandler("q1"));

			_created[0].Mock.Verify(x => x.Stop(), Times.Once);
			Assert.AreEqual(0, _manager.GetUnits().Count);
		}

		[TestMethod]
		public void Reload_ReadsSourceAgain()
		{
			_manager.Start();
			_source.Definitions = ServiceDefinitionSourceTests.CreateDefinitions("q1");

			_manager.Reload();

			Get("q1").Mock.Verify(x => x.StartAsync(), Times.Once);
		}

		[TestMethod]
		public void StopCommand_KeepsCommandStopped()
		{
			var command = ServiceDefinitionFactory.CreateCommand(new Dictionary<string, string>
			{
				["name"] = "c1",
				["type"] = nameof(TokenCommand),
				["schedule"] = "monthly",
				["daysOfMonth"] = "1",
				["at"] = "03:00",
				["startDate"] = "2100-01-01"
			});
			_source.Definitions = new ServiceDefinitions(null, new[] { command });
			_manager.Start();

			Assert.IsTrue(_manager.StopCommand("c1"));

			var state = _manager.GetUnits().Single();
			Assert.AreEqual(ServiceUnitKind.Command, state.Kind);
			Assert.IsFalse(state.IsEnabled);
		}

		private CreatedSubscriber Get(string endpoint)
		{
			return _created.Last(x => x.Endpoint == endpoint);
		}

		private ServiceUnitManager CreateManager(IServicesSettings servicesSettings, IServiceDefinitionSource source)
		{
			return new ServiceUnitManager(
				servicesSettings,
				source,
				null,
				new Mock<ICommandFactory>().Object,
				new RepeaterFactory(),
				_subscriberFactory.Object,
				new Mock<IMessageHandlerFactory>().Object,
				null,
				null);
		}

		private static MessageHandlerSettings CreateHandler(string endpoint, string threads = "1")
		{
			return ServiceDefinitionFactory.CreateHandler(new Dictionary<string, string>
			{
				["name"] = endpoint,
				["type"] = nameof(SyncTestHandler),
				["subscriptionEndpoint"] = endpoint,
				["degreeOfParallelism"] = threads
			});
		}

		private sealed class CreatedSubscriber
		{
			public CreatedSubscriber(string endpoint, Mock<ISubscriber> mock)
			{
				Endpoint = endpoint;
				Mock = mock;
			}

			public string Endpoint { get; }

			public Mock<ISubscriber> Mock { get; }
		}
	}
}
