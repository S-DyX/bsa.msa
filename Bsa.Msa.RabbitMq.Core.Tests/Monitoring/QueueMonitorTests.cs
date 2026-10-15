using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Monitoring;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Bsa.Msa.RabbitMq.Core.Tests.Monitoring
{
	/// <summary>
	/// Tests of the queue monitors and of <see cref="QueueQuery"/>.
	/// </summary>
	[TestClass]
	public sealed class QueueMonitorTests
	{
		private const string ApiAnswer = @"[
			{ ""name"": ""small"", ""vhost"": ""/"", ""state"": ""running"", ""consumers"": 1,
			  ""messages"": 5, ""messages_ready"": 4, ""messages_unacknowledged"": 1,
			  ""message_stats"": { ""publish_details"": { ""rate"": 2.5 }, ""deliver_get_details"": { ""rate"": 1.5 } } },
			{ ""name"": ""big"", ""vhost"": ""/"", ""consumers"": 0, ""messages"": 100, ""messages_ready"": 100, ""messages_unacknowledged"": 0 },
			{ ""name"": ""idle"", ""vhost"": ""/"" }
		]";

		[TestMethod]
		public void Query_Default_LargestFirstThenByName()
		{
			var queues = new[] { Queue("b", 5), Queue("a", 5), Queue("c", 10) };

			var result = queues.Apply(null);

			CollectionAssert.AreEqual(new[] { "c", "a", "b" }, result.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void Query_FilterAndTop()
		{
			var queues = new[] { Queue("orders", 1), Queue("orders.Error", 7), Queue("mail", 50), Queue("orders.Retry", 3) };
			var query = new QueueQuery { NameContains = "ORDERS", MinMessages = 2, Top = 1 };

			var result = queues.Apply(query);

			Assert.AreEqual("orders.Error", result.Single().Name);
		}

		[TestMethod]
		public void Query_SortByNameAscending()
		{
			var queues = new[] { Queue("b", 1), Queue("a", 2) };
			var query = new QueueQuery { SortBy = QueueSortField.Name, Descending = false };

			CollectionAssert.AreEqual(new[] { "a", "b" }, queues.Apply(query).Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void Query_UnknownUnacknowledged_GoesLast()
		{
			var known = Queue("known", 1);
			known.MessagesUnacknowledged = 0;
			var unknown = Queue("unknown", 1);
			var query = new QueueQuery { SortBy = QueueSortField.MessagesUnacknowledged };

			Assert.AreEqual("known", new[] { unknown, known }.Apply(query).First().Name);
		}

		[TestMethod]
		public async Task ManagementApi_ParsesAndSortsQueues()
		{
			var handler = new FakeHttpHandler(HttpStatusCode.OK, ApiAnswer);
			using var monitor = CreateApiMonitor(handler, "/");

			var queues = await monitor.GetQueuesAsync();

			CollectionAssert.AreEqual(new[] { "big", "small", "idle" }, queues.Select(x => x.Name).ToList());
			var small = queues.Single(x => x.Name == "small");
			Assert.AreEqual(5, small.Messages);
			Assert.AreEqual(4, small.MessagesReady);
			Assert.AreEqual(1L, small.MessagesUnacknowledged);
			Assert.AreEqual(1, small.Consumers);
			Assert.AreEqual("running", small.State);
			Assert.AreEqual(2.5, small.PublishRate);
			Assert.AreEqual(1.5, small.DeliverRate);
			var idle = queues.Single(x => x.Name == "idle");
			Assert.AreEqual(0, idle.Messages);
			Assert.IsNull(idle.PublishRate);
		}

		[TestMethod]
		public async Task ManagementApi_RequestsVirtualHostWithBasicAuth()
		{
			var handler = new FakeHttpHandler(HttpStatusCode.OK, "[]");
			using var monitor = CreateApiMonitor(handler, "/");

			await monitor.GetQueuesAsync();

			var request = handler.LastRequest;
			StringAssert.StartsWith(request.RequestUri.AbsoluteUri, "http://rabbit:15672/api/queues/%2F?columns=");
			Assert.AreEqual("Basic", request.Headers.Authorization.Scheme);
			Assert.AreEqual(
				Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret")),
				request.Headers.Authorization.Parameter);
		}

		[TestMethod]
		public async Task ManagementApi_WithoutVirtualHost_ReadsAllQueues()
		{
			var handler = new FakeHttpHandler(HttpStatusCode.OK, "[]");
			using var monitor = CreateApiMonitor(handler, null);

			await monitor.GetQueuesAsync();

			StringAssert.StartsWith(handler.LastRequest.RequestUri.AbsoluteUri, "http://rabbit:15672/api/queues?columns=");
		}

		[TestMethod]
		public async Task ManagementApi_ErrorStatus_Throws()
		{
			var handler = new FakeHttpHandler(HttpStatusCode.Unauthorized, string.Empty);
			using var monitor = CreateApiMonitor(handler, "/");

			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => monitor.GetQueuesAsync());
		}

		[TestMethod]
		public async Task Amqp_ReadsExistingQueuesAndSkipsMissing()
		{
			var model = new Mock<IModel>();
			model.Setup(x => x.QueueDeclarePassive("orders")).Returns(new QueueDeclareOk("orders", 5, 2));
			model.Setup(x => x.QueueDeclarePassive("orders.Error")).Returns(new QueueDeclareOk("orders.Error", 9, 0));
			model
				.Setup(x => x.QueueDeclarePassive("orders.Retry"))
				.Throws(new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Peer, 404, "NOT_FOUND")));
			var connection = new Mock<ISimpleConnection>();
			connection
				.Setup(x => x.Execute(It.IsAny<Action<Func<IModel>>>(), It.IsAny<string>()))
				.Callback<Action<Func<IModel>>, string>((action, name) => action(() => model.Object));
			using var monitor = new AmqpQueueMonitor(
				connection.Object,
				() => AmqpQueueMonitor.WithServiceQueues(new[] { "orders" }),
				"/",
				false);

			var queues = await monitor.GetQueuesAsync();

			CollectionAssert.AreEqual(new[] { "orders.Error", "orders" }, queues.Select(x => x.Name).ToList());
			Assert.AreEqual(2, queues[1].Consumers);
			Assert.IsNull(queues[1].MessagesUnacknowledged);
		}

		private static ManagementApiQueueMonitor CreateApiMonitor(FakeHttpHandler handler, string virtualHost)
		{
			var options = new ManagementApiOptions
			{
				BaseAddress = new Uri("http://rabbit:15672"),
				UserName = "user",
				Password = "secret",
				VirtualHost = virtualHost
			};
			return new ManagementApiQueueMonitor(options, new HttpClient(handler));
		}

		private static QueueInfo Queue(string name, long messages)
		{
			return new QueueInfo { Name = name, Messages = messages, MessagesReady = messages };
		}

		private sealed class FakeHttpHandler : HttpMessageHandler
		{
			private readonly HttpStatusCode _status;

			private readonly string _body;

			public FakeHttpHandler(HttpStatusCode status, string body)
			{
				_status = status;
				_body = body;
			}

			public HttpRequestMessage LastRequest { get; private set; }

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				LastRequest = request;
				var response = new HttpResponseMessage(_status)
				{
					Content = new StringContent(_body, Encoding.UTF8, "application/json")
				};
				return Task.FromResult(response);
			}
		}
	}
}
