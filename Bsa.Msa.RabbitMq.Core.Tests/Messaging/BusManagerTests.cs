using System;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bsa.Msa.RabbitMq.Core.Tests.Messaging
{
	/// <summary>
	/// Tests of <see cref="BusManager"/> routing.
	/// </summary>
	[TestClass]
	public sealed class BusManagerTests
	{
		private const string Queue = "queue";

		private readonly TestMessage _message = new TestMessage { Id = 1 };

		private Mock<ISimpleBus> _simpleBus;

		private Mock<ILocalBus> _localBus;

		private BusManager _manager;

		[TestInitialize]
		public void Initialize()
		{
			_simpleBus = new Mock<ISimpleBus>();
			_simpleBus.Setup(x => x.IsConnected).Returns(true);
			_localBus = new Mock<ILocalBus>();
			_manager = new BusManager(_simpleBus.Object, _localBus.Object);
		}

		[TestMethod]
		public void Send_WithoutQueue_PublishesWithTtl()
		{
			_manager.Send(_message, 100);

			_simpleBus.Verify(x => x.Publish(_message, string.Empty, null, 100), Times.Once);
		}

		[TestMethod]
		public void Send_HandledLocally_DoesNotUseBroker()
		{
			_localBus.Setup(x => x.Handle(Queue, _message)).Returns(true);

			_manager.Send(Queue, _message, null, false);

			_simpleBus.Verify(x => x.Send(It.IsAny<string>(), It.IsAny<TestMessage>(), It.IsAny<int?>()), Times.Never);
		}

		[TestMethod]
		public void Send_ForceSend_SkipsLocalBus()
		{
			_manager.Send(Queue, _message, 5, true);

			_localBus.Verify(x => x.Handle(It.IsAny<string>(), It.IsAny<TestMessage>()), Times.Never);
			_simpleBus.Verify(x => x.Send(Queue, _message, 5), Times.Once);
		}

		[TestMethod]
		public void Send_LocalHandlerFailed_SendsThroughBroker()
		{
			_localBus.Setup(x => x.Handle(Queue, _message)).Throws(new InvalidOperationException());

			_manager.Send(Queue, _message, null, false);

			_simpleBus.Verify(x => x.Send(Queue, _message, null), Times.Once);
		}

		[TestMethod]
		public void Send_BrokerFailed_ThrowsWithoutRetry()
		{
			_simpleBus
				.Setup(x => x.Send(Queue, _message, null))
				.Throws(new InvalidOperationException());

			Assert.ThrowsException<InvalidOperationException>(
				() => _manager.Send(Queue, _message, null, false));

			_simpleBus.Verify(x => x.Send(Queue, _message, null), Times.Once);
		}

		[TestMethod]
		public void Dispose_DoesNotReconnect()
		{
			_simpleBus.Setup(x => x.IsConnected).Returns(false);

			_manager.Dispose();

			_simpleBus.Verify(x => x.Dispose(), Times.Once);
			_simpleBus.Verify(x => x.Reconnect(It.IsAny<string>()), Times.Never);
		}
	}
}
