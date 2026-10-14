using System;
using System.Threading;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Impl;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bsa.Msa.RabbitMq.Core.Tests.Handlers
{
	/// <summary>
	/// Tests of <see cref="CommandFactory"/> and <see cref="MessageHandlerFactory"/>.
	/// </summary>
	[TestClass]
	public sealed class FactoryTests
	{
		[TestMethod]
		public void CommandFactory_UsesLongestConstructorAndPassesToken()
		{
			var registry = new Mock<ICommandRegistry>();
			registry.Setup(x => x.Resolve("TokenCommand")).Returns(typeof(TokenCommand));
			var settings = new Mock<ISettings>().Object;
			using var cts = new CancellationTokenSource();
			var factory = new CommandFactory(registry.Object);

			var command = (TokenCommand)factory.Create("TokenCommand", settings, cts.Token);

			Assert.AreSame(settings, command.Settings);
			Assert.AreEqual(cts.Token, command.Token);
		}

		[TestMethod]
		public void CommandFactory_UnknownType_Throws()
		{
			var registry = new Mock<ICommandRegistry>();
			var factory = new CommandFactory(registry.Object);

			Assert.ThrowsException<InvalidOperationException>(
				() => factory.Create("Unknown", null, CancellationToken.None));
		}

		[TestMethod]
		public void HandlerFactory_CreatesRpcHandler()
		{
			var factory = CreateHandlerFactory("Echo", typeof(EchoRequestHandler));

			var handler = factory.Create<TestRequest, TestResponse>("Echo", null, null, null);

			Assert.AreEqual("hi", handler.Handle(new TestRequest { Text = "hi" }).Text);
		}

		[TestMethod]
		public void HandlerFactory_WrongRpcInterface_Throws()
		{
			var factory = CreateHandlerFactory("Sync", typeof(SyncTestHandler));

			Assert.ThrowsException<InvalidOperationException>(
				() => factory.Create<TestRequest, TestResponse>("Sync", null, null, null));
		}

		[TestMethod]
		public void HandlerFactory_InjectsBusAndSettingsIntoLongestConstructor()
		{
			var factory = CreateHandlerFactory("Dependent", typeof(DependentTestHandler));
			var settings = HandlerSettingsFactory.Create("Dependent", "queue");
			var simpleBus = new Mock<ISimpleBus>().Object;
			var localBus = new Mock<ILocalBus>().Object;

			var handler = (DependentTestHandler)factory.Create<TestMessage>(
				"Dependent",
				settings,
				simpleBus,
				localBus);

			Assert.IsInstanceOfType(handler.BusManager, typeof(BusManager));
			Assert.AreSame(settings, handler.Settings);
		}

		private static MessageHandlerFactory CreateHandlerFactory(string name, Type handlerType)
		{
			var registry = new Mock<IHandlerRegistry>();
			registry.Setup(x => x.ResolveHandler(name)).Returns(handlerType);
			var container = new Mock<ILocalContainer>();
			return new MessageHandlerFactory(registry.Object, container.Object);
		}
	}
}
