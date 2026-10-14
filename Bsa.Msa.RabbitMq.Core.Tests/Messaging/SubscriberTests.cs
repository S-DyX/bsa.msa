using System;
using System.Collections.Generic;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bsa.Msa.RabbitMq.Core.Tests.Messaging
{
	/// <summary>
	/// Tests of <see cref="SubscriberBase{TMessage}"/> and
	/// <see cref="RespondBase{TMessage, TResponse}"/>.
	/// </summary>
	[TestClass]
	public sealed class SubscriberTests
	{
		[TestMethod]
		public void SubscriberBase_StartSubscribesStopDisposesBus()
		{
			var settings = new MessageHandlerSettings(ConfigurationFactory.CreateSection(
				new Dictionary<string, string>
				{
					["type"] = nameof(SyncTestHandler),
					["subscriptionEndpoint"] = "queue"
				}));
			var factory = new Mock<IMessageHandlerFactory>();
			factory
				.Setup(x => x.Create<TestMessage>(It.IsAny<string>(), It.IsAny<ISettings>(), It.IsAny<ISimpleBus>(), It.IsAny<ILocalBus>()))
				.Returns(new SyncTestHandler());
			var simpleBus = new Mock<ISimpleBus>();
			simpleBus.SetupProperty(x => x.Shutdown);
			var localBus = new Mock<ILocalBus>();
			var subscriber = new SubscriberBase<TestMessage>(
				settings,
				factory.Object,
				null,
				simpleBus.Object,
				localBus.Object);

			subscriber.Start();
			subscriber.Stop();

			localBus.Verify(x => x.Register(settings), Times.Once);
			simpleBus.Verify(
				x => x.Subscribe("queue", It.IsAny<Action<TestMessage>>(), settings),
				Times.Once);
			simpleBus.Verify(x => x.Dispose(), Times.Once);
			Assert.IsNull(simpleBus.Object.Shutdown);
		}

		[TestMethod]
		public void RespondBase_AnswersThroughHandlerAndStops()
		{
			Func<TestRequest, TestResponse> respond = null;
			var subscription = new Mock<IDisposable>();
			var busManager = new Mock<IBusManager>();
			busManager
				.Setup(x => x.Respond(It.IsAny<Func<TestRequest, TestResponse>>(), "rpc"))
				.Callback<Func<TestRequest, TestResponse>, string>((func, queue) => respond = func)
				.Returns(subscription.Object);
			var responder = new RespondBase<TestRequest, TestResponse>(
				busManager.Object,
				CreateRpcSettings(),
				CreateRpcFactory(new EchoRequestHandler()).Object);

			responder.Start();

			Assert.IsTrue(responder.IsStarted);
			Assert.AreEqual("hi", respond(new TestRequest { Text = "hi" }).Text);

			responder.Stop();

			Assert.IsFalse(responder.IsStarted);
			subscription.Verify(x => x.Dispose(), Times.Once);
		}

		[TestMethod]
		public void RespondBase_FactoryFailed_RaisesOnError()
		{
			var factory = new Mock<IMessageHandlerFactory>();
			factory
				.Setup(x => x.Create<TestRequest, TestResponse>(It.IsAny<string>(), It.IsAny<ISettings>(), It.IsAny<ISimpleBus>(), It.IsAny<ILocalBus>()))
				.Throws(new InvalidOperationException());
			var responder = new RespondBase<TestRequest, TestResponse>(
				new Mock<IBusManager>().Object,
				CreateRpcSettings(),
				factory.Object);
			Exception error = null;
			responder.OnError += (sender, e) => error = (Exception)e.ExceptionObject;

			responder.Start();

			Assert.IsInstanceOfType(error, typeof(InvalidOperationException));
			Assert.IsFalse(responder.IsStarted);
		}

		private static IMessageHandlerSettings CreateRpcSettings()
		{
			return HandlerSettingsFactory.Create(nameof(EchoRequestHandler), "rpc");
		}

		private static Mock<IMessageHandlerFactory> CreateRpcFactory(EchoRequestHandler handler)
		{
			var factory = new Mock<IMessageHandlerFactory>();
			factory
				.Setup(x => x.Create<TestRequest, TestResponse>(It.IsAny<string>(), It.IsAny<ISettings>(), It.IsAny<ISimpleBus>(), It.IsAny<ILocalBus>()))
				.Returns(handler);
			return factory;
		}
	}
}
