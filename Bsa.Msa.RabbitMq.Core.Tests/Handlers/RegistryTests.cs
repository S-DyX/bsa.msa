using System;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Handlers
{
	/// <summary>
	/// Tests of the type scanning: <see cref="HandlerRegistry"/>,
	/// <see cref="CommandRegistry"/>, <see cref="TypeNameMap{TValue}"/>
	/// and <see cref="AssemblyHelpers"/>.
	/// </summary>
	[TestClass]
	public sealed class RegistryTests
	{
		private static HandlerRegistry _handlerRegistry;

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			// Scanning the assemblies is slow, one registry is shared.
			_handlerRegistry = new HandlerRegistry();
		}

		[TestMethod]
		public void HandlerRegistry_FindsHandlerAndMessageByShortName()
		{
			Assert.AreEqual(typeof(SyncTestHandler), _handlerRegistry.ResolveHandler(nameof(SyncTestHandler)));
			Assert.AreEqual(typeof(TestMessage), _handlerRegistry.ResolveMessage(nameof(SyncTestHandler)));
		}

		[TestMethod]
		public void HandlerRegistry_AsyncHandler_IsFound()
		{
			Assert.AreEqual(typeof(TestMessage), _handlerRegistry.ResolveMessage(nameof(AsyncTestHandler)));
		}

		[TestMethod]
		public void HandlerRegistry_RpcHandler_MessageIsRequest()
		{
			Assert.AreEqual(typeof(TestRequest), _handlerRegistry.ResolveMessage(nameof(EchoRequestHandler)));
		}

		[TestMethod]
		public void HandlerRegistry_DuplicateShortName_IsAmbiguous()
		{
			Assert.ThrowsException<InvalidOperationException>(
				() => _handlerRegistry.ResolveHandler("DuplicateHandler"));
			Assert.AreEqual(
				typeof(Fakes.First.DuplicateHandler),
				_handlerRegistry.ResolveHandler(typeof(Fakes.First.DuplicateHandler).FullName));
		}

		[TestMethod]
		public void HandlerRegistry_UnknownName_ReturnsNull()
		{
			Assert.IsNull(_handlerRegistry.ResolveHandler("NoSuchHandler"));
		}

		[TestMethod]
		public void CommandRegistry_FindsCommand()
		{
			var registry = new CommandRegistry();

			Assert.AreEqual(typeof(TokenCommand), registry.Resolve(nameof(TokenCommand)));
		}

		[TestMethod]
		public void TypeNameMap_ExplicitNameResolvesAmbiguity()
		{
			var map = new TypeNameMap<Type>();
			map.Add(typeof(Fakes.First.DuplicateHandler), typeof(Fakes.First.DuplicateHandler));
			map.Add(typeof(Fakes.Second.DuplicateHandler), typeof(Fakes.Second.DuplicateHandler));

			Assert.ThrowsException<InvalidOperationException>(() => map.Find("DuplicateHandler"));

			map.Set("DuplicateHandler", typeof(Fakes.Second.DuplicateHandler));

			Assert.AreEqual(typeof(Fakes.Second.DuplicateHandler), map.Find("DuplicateHandler"));
		}

		[TestMethod]
		public void TypeNameMap_SameTypeTwice_IsNotAmbiguous()
		{
			var map = new TypeNameMap<Type>();
			map.Add(typeof(SyncTestHandler), typeof(SyncTestHandler));
			map.Add(typeof(SyncTestHandler), typeof(SyncTestHandler));

			Assert.AreEqual(typeof(SyncTestHandler), map.Find(nameof(SyncTestHandler)));
		}

		[TestMethod]
		public void AssemblyHelpers_ReturnsEachTypeOnce()
		{
			var types = AssemblyHelpers.GetTypes(x => x == typeof(SyncTestHandler));

			Assert.AreEqual(1, types.Count);
		}
	}
}
