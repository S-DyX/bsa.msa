using System;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bsa.Msa.RabbitMq.Core.Tests.Messaging
{
	/// <summary>
	/// Tests of <see cref="LocalBus"/>.
	/// </summary>
	[TestClass]
	public sealed class LocalBusTests
	{
		private const string Endpoint = "local.queue";

		[TestMethod]
		public void Handle_NotRegisteredEndpoint_ReturnsFalse()
		{
			var bus = new LocalBus(new Mock<IMessageHandlerFactory>().Object);

			Assert.IsFalse(bus.Handle(Endpoint, new TestMessage()));
		}

		[TestMethod]
		public void Handle_SyncHandler_HandlesEveryMessageWithOneInstance()
		{
			var handler = new SyncTestHandler();
			var factory = CreateFactory(() => handler);
			var bus = CreateBus(factory.Object);

			Assert.IsTrue(bus.Handle(Endpoint, new TestMessage()));
			Assert.IsTrue(bus.Handle(Endpoint, new TestMessage()));

			Assert.AreEqual(2, handler.HandledCount);
			factory.Verify(
				x => x.Create<TestMessage>(It.IsAny<string>(), It.IsAny<ISettings>(), It.IsAny<ISimpleBus>(), It.IsAny<ILocalBus>()),
				Times.Once);
		}

		[TestMethod]
		public void Handle_AsyncHandler_WaitsForCompletion()
		{
			var handler = new AsyncTestHandler();
			var bus = CreateBus(CreateFactory(() => handler).Object);

			Assert.IsTrue(bus.Handle(Endpoint, new TestMessage()));

			Assert.IsTrue(handler.IsHandled);
		}

		[TestMethod]
		public void Handle_HandlerOfOtherMessage_ReturnsFalse()
		{
			var bus = CreateBus(CreateFactory(() => new SyncTestHandler()).Object);

			Assert.IsFalse(bus.Handle(Endpoint, new TestRequest()));
		}

		[TestMethod]
		public void Handle_FactoryFailed_NextCallRetries()
		{
			var calls = 0;
			var handler = new SyncTestHandler();
			var factory = CreateFactory(() =>
			{
				if (++calls == 1)
					throw new InvalidOperationException("Not ready.");
				return handler;
			});
			var bus = CreateBus(factory.Object);

			Assert.ThrowsException<InvalidOperationException>(
				() => bus.Handle(Endpoint, new TestMessage()));
			Assert.IsTrue(bus.Handle(Endpoint, new TestMessage()));

			Assert.AreEqual(1, handler.HandledCount);
		}

		private static LocalBus CreateBus(IMessageHandlerFactory factory)
		{
			var settings = HandlerSettingsFactory.Create("TestHandler", Endpoint);
			var bus = new LocalBus(factory);
			bus.Register(settings);
			return bus;
		}

		private static Mock<IMessageHandlerFactory> CreateFactory(Func<IMessageHandler> create)
		{
			var factory = new Mock<IMessageHandlerFactory>();
			factory
				.Setup(x => x.Create<TestMessage>(It.IsAny<string>(), It.IsAny<ISettings>(), It.IsAny<ISimpleBus>(), It.IsAny<ILocalBus>()))
				.Returns(create);
			factory
				.Setup(x => x.Create<TestRequest>(It.IsAny<string>(), It.IsAny<ISettings>(), It.IsAny<ISimpleBus>(), It.IsAny<ILocalBus>()))
				.Returns(create);
			return factory;
		}
	}
}
