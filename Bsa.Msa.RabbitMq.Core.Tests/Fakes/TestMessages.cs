using System;
using System.Threading.Tasks;
using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes
{
	/// <summary>
	/// Message used by the tests.
	/// </summary>
	public sealed class TestMessage
	{
		/// <summary>
		/// Message id.
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Message text.
		/// </summary>
		public string Text { get; set; }
	}

	/// <summary>
	/// Generic argument used by the naming tests.
	/// </summary>
	public sealed class TestItem
	{
	}

	/// <summary>
	/// Generic message used by the naming tests.
	/// </summary>
	/// <typeparam name="T">Payload type.</typeparam>
	public sealed class TestEnvelope<T>
	{
		/// <summary>
		/// Payload.
		/// </summary>
		public T Value { get; set; }
	}

	/// <summary>
	/// RPC request used by the tests.
	/// </summary>
	public sealed class TestRequest
	{
		/// <summary>
		/// Request text.
		/// </summary>
		public string Text { get; set; }
	}

	/// <summary>
	/// RPC response used by the tests.
	/// </summary>
	public sealed class TestResponse
	{
		/// <summary>
		/// Response text.
		/// </summary>
		public string Text { get; set; }
	}

	/// <summary>
	/// Synchronous handler that counts the handled messages.
	/// </summary>
	public sealed class SyncTestHandler : IMessageHandler<TestMessage>
	{
		/// <summary>
		/// Number of handled messages.
		/// </summary>
		public int HandledCount { get; private set; }

		/// <inheritdoc/>
		public void Handle(TestMessage message)
		{
			HandledCount++;
		}
	}

	/// <summary>
	/// Asynchronous handler that finishes after a short delay.
	/// </summary>
	public sealed class AsyncTestHandler : IMessageHandlerAsync<TestMessage>
	{
		/// <summary>
		/// Set when the handler has finished.
		/// </summary>
		public bool IsHandled { get; private set; }

		/// <inheritdoc/>
		public async Task HandleAsync(TestMessage message)
		{
			await Task.Delay(50).ConfigureAwait(false);
			IsHandled = true;
		}
	}

	/// <summary>
	/// RPC handler that echoes the request text.
	/// </summary>
	public sealed class EchoRequestHandler : IMessageHandler<TestRequest, TestResponse>
	{
		/// <inheritdoc/>
		public TestResponse Handle(TestRequest message)
		{
			return new TestResponse { Text = message.Text };
		}
	}

	/// <summary>
	/// Handler that receives its dependencies through the constructor.
	/// </summary>
	public sealed class DependentTestHandler : IMessageHandler<TestMessage>
	{
		/// <summary>
		/// Used when the factory picks the wrong constructor.
		/// </summary>
		/// <param name="busManager">Message bus.</param>
		public DependentTestHandler(IBusManagerMarker busManager)
		{
			throw new InvalidOperationException("The constructor with fewer parameters must not be used.");
		}

		/// <summary>
		/// The constructor with the most parameters.
		/// </summary>
		/// <param name="busManager">Message bus.</param>
		/// <param name="settings">Handler settings.</param>
		public DependentTestHandler(
			Interfaces.IBusManager busManager,
			IMessageHandlerSettings settings)
		{
			BusManager = busManager;
			Settings = settings;
		}

		/// <summary>
		/// Injected bus.
		/// </summary>
		public Interfaces.IBusManager BusManager { get; }

		/// <summary>
		/// Injected settings.
		/// </summary>
		public IMessageHandlerSettings Settings { get; }

		/// <inheritdoc/>
		public void Handle(TestMessage message)
		{
		}
	}

	/// <summary>
	/// Marker type for the constructor selection test.
	/// </summary>
	public interface IBusManagerMarker
	{
	}
}
