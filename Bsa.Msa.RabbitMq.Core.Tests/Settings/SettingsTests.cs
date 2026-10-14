using System.Collections.Generic;
using System.Text.Json;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.RabbitMq.Core.Common;
using Bsa.Msa.RabbitMq.Core.Settings;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Settings
{
	/// <summary>
	/// Tests of the settings parsing and of the restored message headers.
	/// </summary>
	[TestClass]
	public sealed class SettingsTests
	{
		[TestMethod]
		public void MessageHandlerSettings_ParsesRetryAndPostfix()
		{
			var section = ConfigurationFactory.CreateSection(new Dictionary<string, string>
			{
				["name"] = "handler",
				["type"] = "SyncTestHandler",
				["subscriptionEndpoint"] = "orders",
				["postfix"] = "eu",
				["retry"] = "true",
				["retryCount"] = "3",
				["retryDelay"] = "1500",
				["useExchange"] = "true"
			});

			var settings = new MessageHandlerSettings(section);

			Assert.AreEqual("orders.eu", settings.SubscriptionEndpoint);
			Assert.IsTrue(settings.Retry);
			Assert.AreEqual(3, settings.RetryCount);
			Assert.AreEqual(1500, settings.RetryDelay);
			Assert.IsTrue(settings.UseExchange);
			Assert.AreEqual(1, settings.DegreeOfParallelism);
			Assert.AreEqual((ushort)5, settings.PrefetchCount);
		}

		[TestMethod]
		public void MessageHandlerSettings_RetryDelayNotSet_IsNull()
		{
			var section = ConfigurationFactory.CreateSection(new Dictionary<string, string>
			{
				["type"] = "SyncTestHandler"
			});

			IMessageHandlerSettings settings = new MessageHandlerSettings(section);

			Assert.IsNull(settings.RetryDelay);
		}

		[TestMethod]
		public void RabbitMqSettings_ParsesConnectionString()
		{
			var settings = new RabbitMqSettings("host=rmq;username=user;password=secret;virtualHost=vh");

			Assert.AreEqual("rmq", settings.Host);
			Assert.AreEqual("user", settings.UserName);
			Assert.AreEqual("secret", settings.Password);
			Assert.AreEqual("vh", settings.VirtualHost);
		}

		[TestMethod]
		public void NormalizeHeaders_JsonValuesBecomeSendable()
		{
			var json = JsonDocument.Parse(
				"{\"int\":1,\"long\":5000000000,\"text\":\"a\",\"flag\":true,\"list\":[1]}").RootElement;
			var headers = new Dictionary<string, object>
			{
				["int"] = json.GetProperty("int"),
				["long"] = json.GetProperty("long"),
				["text"] = json.GetProperty("text"),
				["flag"] = json.GetProperty("flag"),
				["list"] = json.GetProperty("list"),
				["bytes"] = new byte[] { 1 }
			};

			var result = InternalBus.NormalizeHeaders(headers);

			Assert.AreEqual(1, result["int"]);
			Assert.AreEqual(5000000000L, result["long"]);
			Assert.AreEqual("a", result["text"]);
			Assert.AreEqual(true, result["flag"]);
			Assert.AreEqual("[1]", result["list"]);
			Assert.IsInstanceOfType(result["bytes"], typeof(byte[]));
		}

		[TestMethod]
		public void NormalizeHeaders_Null_ReturnsEmpty()
		{
			Assert.AreEqual(0, InternalBus.NormalizeHeaders(null).Count);
		}
	}
}
