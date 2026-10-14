using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes.First
{
	/// <summary>
	/// Handler whose short name clashes with
	/// <see cref="Second.DuplicateHandler"/>.
	/// </summary>
	public sealed class DuplicateHandler : IMessageHandler<TestMessage>
	{
		/// <inheritdoc/>
		public void Handle(TestMessage message)
		{
		}
	}
}

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes.Second
{
	/// <summary>
	/// Handler whose short name clashes with
	/// <see cref="First.DuplicateHandler"/>.
	/// </summary>
	public sealed class DuplicateHandler : IMessageHandler<TestMessage>
	{
		/// <inheritdoc/>
		public void Handle(TestMessage message)
		{
		}
	}
}
