using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Messaging
{
	/// <summary>
	/// Tests of the file-based <see cref="ExternalProxyBus"/>.
	/// </summary>
	[TestClass]
	public sealed class ExternalProxyBusTests
	{
		private const string Queue = "queue";

		private string _path;

		private ExternalProxyBus _bus;

		[TestInitialize]
		public void Initialize()
		{
			_path = Path.Combine(Path.GetTempPath(), $"msa-tests-{Guid.NewGuid():N}");
			_bus = new ExternalProxyBus(_path);
		}

		[TestCleanup]
		public void Cleanup()
		{
			_bus.Dispose();
			if (Directory.Exists(_path))
				Directory.Delete(_path, true);
		}

		[TestMethod]
		public void Send_WithSubscriber_DeliversAndConsumes()
		{
			var received = new List<TestMessage>();
			_bus.Subscribe<TestMessage>(Queue, received.Add, null);

			_bus.Send(Queue, new TestMessage { Id = 1 });

			Assert.AreEqual(1, received.Count);
			Assert.AreEqual(0, _bus.GetMessageExchange<TestMessage>(Queue).Count);
		}

		[TestMethod]
		public void Send_WithoutSubscriber_KeptUntilRead()
		{
			_bus.Send(Queue, new TestMessage { Id = 1 });
			_bus.Send(Queue, new TestMessage { Id = 2 });

			var first = _bus.GetMessageExchange<TestMessage>(Queue, 1);
			var rest = _bus.GetMessageExchange<TestMessage>(Queue);

			Assert.AreEqual(1, first.Count);
			Assert.AreEqual(1, rest.Count);
			Assert.AreNotEqual(first[0].Id, rest[0].Id);
			Assert.AreEqual(0, _bus.GetMessageExchange<TestMessage>(Queue).Count);
		}

		[TestMethod]
		public void Send_SubscriberFailed_MessageStaysInQueue()
		{
			_bus.Subscribe<TestMessage>(Queue, x => throw new InvalidOperationException(), null);

			_bus.Send(Queue, new TestMessage { Id = 1 });

			Assert.AreEqual(1, _bus.GetMessageExchange<TestMessage>(Queue).Count);
		}

		[TestMethod]
		public void Send_TtlInMilliseconds_ExpiredMessageDropped()
		{
			_bus.Send(Queue, new TestMessage(), 1);
			Thread.Sleep(50);

			Assert.AreEqual(0, _bus.GetMessageExchange<TestMessage>(Queue).Count);
		}

		[TestMethod]
		public void Publish_DeliversToBoundQueue()
		{
			var received = new List<TestMessage>();
			_bus.SubscribeExchange<TestMessage>("bound", received.Add, null);

			_bus.Publish(new TestMessage { Id = 3 });

			Assert.AreEqual(3, received[0].Id);
		}

		[TestMethod]
		public void Send_DefaultQueueName_WorksAsFileName()
		{
			// The default name contains ':' which Windows does not allow in paths.
			_bus.Send(new TestMessage { Id = 4 });

			var queue = new DefaultSimpleBusNaming().GetQueueName<TestMessage>();
			Assert.AreEqual(4, _bus.GetMessageExchange<TestMessage>(queue)[0].Id);
		}

		[TestMethod]
		public void Purge_RemovesMessages()
		{
			_bus.Send(Queue, new TestMessage());

			_bus.Purge(Queue);

			Assert.AreEqual(0, _bus.GetMessageExchange<TestMessage>(Queue).Count);
		}

		[TestMethod]
		public void Respond_WritesResponse()
		{
			using var subscription = _bus.Respond<TestRequest, TestResponse>(
				x => new TestResponse { Text = x.Text },
				"rpc");

			_bus.Send("rpc", new TestRequest { Text = "hi" });

			var responses = _bus.GetMessageExchange<TestResponse>("rpc.responses");
			Assert.AreEqual("hi", responses[0].Text);
		}
	}
}
