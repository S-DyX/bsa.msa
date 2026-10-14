using Bsa.Msa.RabbitMq.Core.Tests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bsa.Msa.RabbitMq.Core.Tests.Naming
{
	/// <summary>
	/// Tests of the queue and exchange naming.
	/// </summary>
	[TestClass]
	public sealed class NamingTests
	{
		private const string Assembly = "Bsa.Msa.RabbitMq.Core.Tests";

		private const string Namespace = "Bsa.Msa.RabbitMq.Core.Tests.Fakes";

		[TestMethod]
		public void GetQueueName_PlainType_FullNameAndAssembly()
		{
			Assert.AreEqual(
				$"{Namespace}.TestMessage:{Assembly}",
				SimpleBusExtension.GetQueueName<TestMessage>());
		}

		[TestMethod]
		public void GetQueueName_GenericType_ReadableWithoutVersions()
		{
			var name = SimpleBusExtension.GetQueueName<TestEnvelope<TestItem>>();

			Assert.AreEqual($"{Namespace}.TestEnvelope[{Namespace}.TestItem]:{Assembly}", name);
			Assert.IsFalse(name.Contains("Version="));
		}

		[TestMethod]
		public void GetTypeName_NestedGenerics()
		{
			var name = SimpleBusExtension.GetTypeName(typeof(TestEnvelope<TestEnvelope<TestItem>>));

			Assert.AreEqual(
				$"{Namespace}.TestEnvelope[{Namespace}.TestEnvelope[{Namespace}.TestItem]]",
				name);
		}

		[TestMethod]
		public void GetExchangeName_HasPrefix()
		{
			Assert.AreEqual(
				$"Exchange:{Namespace}.TestMessage:{Assembly}",
				SimpleBusExtension.GetExchangeName<TestMessage>());
		}

		[TestMethod]
		public void EasyNetQNaming_GenericType()
		{
			var naming = new EasyNetQSimpleBusNaming();

			Assert.AreEqual(
				$"{Namespace}.TestEnvelope[{Namespace}.TestItem], {Assembly}",
				naming.GetQueueName<TestEnvelope<TestItem>>());
		}
	}
}
