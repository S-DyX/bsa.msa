using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bsa.Msa.RabbitMq.Core.Tests.Messaging
{
	/// <summary>
	/// Tests of <see cref="SimpleBus"/> on a mocked channel: acknowledgement,
	/// error queue, retries, TTL and RPC.
	/// </summary>
	[TestClass]
	public sealed class SimpleBusTests
	{
		private ChannelHarness _harness;

		private SimpleBus _bus;

		private string _queue;

		[TestInitialize]
		public void Initialize()
		{
			_harness = new ChannelHarness();
			_bus = new SimpleBus(_harness.Connection.Object, null, null, null);

			// The local queue is shared by the process, a unique name isolates the test.
			_queue = $"test.{Guid.NewGuid():N}";
		}

		[TestMethod]
		public void Received_ValidMessage_HandledAndAcked()
		{
			TestMessage received = null;
			Subscribe(x => received = x);

			_harness.Deliver(1, "{\"Id\":7,\"Text\":\"a\"}");

			Assert.AreEqual(7, received?.Id);
			CollectionAssert.AreEqual(new ulong[] { 1 }, _harness.Acks);
			Assert.AreEqual(0, _harness.Published.Count);
		}

		[TestMethod]
		public void Received_MalformedJson_MovedToErrorQueueAndAcked()
		{
			var isHandled = false;
			Subscribe(x => isHandled = true);

			_harness.Deliver(2, "not json");

			Assert.IsFalse(isHandled);
			var error = _harness.Published.Single();
			Assert.AreEqual($"{_queue}.Error", error.RoutingKey);
			StringAssert.Contains(error.Body, "not json");
			CollectionAssert.AreEqual(new ulong[] { 2 }, _harness.Acks);
		}

		[TestMethod]
		public void Received_HandlerFailedWithoutRetry_MovedToErrorQueue()
		{
			Subscribe(x => throw new InvalidOperationException("boom"));

			_harness.Deliver(3, "{\"Id\":1}");

			Assert.AreEqual($"{_queue}.Error", _harness.Published.Single().RoutingKey);
			CollectionAssert.AreEqual(new ulong[] { 3 }, _harness.Acks);
		}

		[TestMethod]
		public void Received_HandlerFailed_RepublishedWithRetryCount()
		{
			Subscribe(x => throw new InvalidOperationException("boom"), ("retry", "true"), ("retryCount", "2"));

			_harness.Deliver(4, "{\"Id\":1}");

			var retry = _harness.Published.Single();
			Assert.AreEqual(_queue, retry.RoutingKey);
			Assert.AreEqual(1, retry.Properties.Headers["retryCount"]);
			CollectionAssert.AreEqual(new ulong[] { 4 }, _harness.Acks);
		}

		[TestMethod]
		public void Received_RetriesExhausted_MovedToErrorQueue()
		{
			Subscribe(x => throw new InvalidOperationException("boom"), ("retry", "true"), ("retryCount", "2"));

			// Other clients send header strings as byte[].
			_harness.Deliver(5, "{\"Id\":1}", p => p.Headers = new Dictionary<string, object>
			{
				["retryCount"] = Encoding.UTF8.GetBytes("2")
			});

			Assert.AreEqual($"{_queue}.Error", _harness.Published.Single().RoutingKey);
		}

		[TestMethod]
		public void Received_RetryDelay_GoesThroughRetryQueue()
		{
			Subscribe(
				x => throw new InvalidOperationException("boom"),
				("retry", "true"),
				("retryCount", "2"),
				("retryDelay", "500"));

			_harness.Deliver(6, "{\"Id\":1}");

			var retry = _harness.Published.Single();
			var retryQueue = $"{_queue}.Retry";
			Assert.AreEqual(retryQueue, retry.RoutingKey);
			Assert.AreEqual("500", retry.Properties.Expiration);
			Assert.AreEqual(_queue, _harness.DeclaredQueues[retryQueue]["x-dead-letter-routing-key"]);
		}

		[TestMethod]
		public void Send_WithTtl_SetsExpirationAndKeepsQueue()
		{
			_bus.Send("out", new TestMessage { Id = 1 }, 1000);

			var message = _harness.Published.Single();
			Assert.AreEqual("out", message.RoutingKey);
			Assert.AreEqual("1000", message.Properties.Expiration);
			_harness.Model.Verify(
				x => x.QueueDelete(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()),
				Times.Never);
		}

		[TestMethod]
		public void Publish_WithTtl_SetsExpiration()
		{
			_bus.Publish(new TestMessage(), string.Empty, "exchange", 500);

			var message = _harness.Published.Single();
			Assert.AreEqual("exchange", message.Exchange);
			Assert.AreEqual("500", message.Properties.Expiration);
		}

		[TestMethod]
		public void Respond_AnswersToReplyQueueAndCancelsOnDispose()
		{
			var subscription = _bus.Respond<TestRequest, TestResponse>(
				x => new TestResponse { Text = x.Text + "!" },
				"rpc");

			_harness.Deliver(8, "{\"Text\":\"hi\"}", p =>
			{
				p.ReplyTo = "reply";
				p.CorrelationId = "c1";
			});
			subscription.Dispose();

			var response = _harness.Published.Single();
			Assert.AreEqual("reply", response.RoutingKey);
			Assert.AreEqual("c1", response.Properties.CorrelationId);
			StringAssert.Contains(response.Body, "hi!");
			CollectionAssert.AreEqual(new ulong[] { 8 }, _harness.Acks);
			_harness.Model.Verify(x => x.BasicCancel(ChannelHarness.ConsumerTag), Times.Once);
		}

		[TestMethod]
		[DataRow(3)]
		[DataRow(3L)]
		[DataRow("3")]
		public void GetRetryCount_ReadsValue(object value)
		{
			var headers = new Dictionary<string, object> { ["retryCount"] = value };

			Assert.AreEqual(3, SimpleBus.GetRetryCount(headers));
		}

		[TestMethod]
		public void GetRetryCount_BytesAndJson()
		{
			var bytes = new Dictionary<string, object> { ["retryCount"] = Encoding.UTF8.GetBytes("4") };
			var json = new Dictionary<string, object>
			{
				["retryCount"] = JsonDocument.Parse("5").RootElement
			};

			Assert.AreEqual(4, SimpleBus.GetRetryCount(bytes));
			Assert.AreEqual(5, SimpleBus.GetRetryCount(json));
		}

		[TestMethod]
		public void GetRetryCount_MissingOrInvalid_IsZero()
		{
			var invalid = new Dictionary<string, object> { ["retryCount"] = "x" };

			Assert.AreEqual(0, SimpleBus.GetRetryCount(null));
			Assert.AreEqual(0, SimpleBus.GetRetryCount(new Dictionary<string, object>()));
			Assert.AreEqual(0, SimpleBus.GetRetryCount(invalid));
		}

		private void Subscribe(
			Action<TestMessage> action,
			params (string Key, string Value)[] settings)
		{
			var values = new Dictionary<string, string>
			{
				["name"] = "test",
				["type"] = nameof(SyncTestHandler),
				["subscriptionEndpoint"] = _queue,
				["degreeOfParallelism"] = "1"
			};
			foreach (var (key, value) in settings)
				values[key] = value;

			var handlerSettings = new MessageHandlerSettings(ConfigurationFactory.CreateSection(values));
			_bus.Subscribe(_queue, action, handlerSettings);
		}
	}
}
