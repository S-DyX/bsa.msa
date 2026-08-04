using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	/// <summary>
	/// Defines a local message bus for handling messages within the application boundary.
	/// </summary>
	public interface ILocalBus
	{
		/// <summary>
		/// Registers message handler settings with the bus.
		/// </summary>
		/// <param name="settings">The settings defining how messages are handled.</param>
		void Register(IMessageHandlerSettings settings);

		/// <summary>
		/// Handles a message of type <typeparamref name="TMessage"/> targeted to a specific subscription endpoint.
		/// </summary>
		/// <typeparam name="TMessage">The type of the message to handle.</typeparam>
		/// <param name="subscriptionEndpoint">The endpoint (e.g., queue or topic name) where the message is sent.</param>
		/// <param name="message">The message instance to be processed.</param>
		/// <returns>True if the message was handled successfully; otherwise, false.</returns>
		bool Handle<TMessage>(string subscriptionEndpoint, TMessage message);
	}


}
