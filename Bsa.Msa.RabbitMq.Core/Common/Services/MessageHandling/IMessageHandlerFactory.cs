using System;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core.Interfaces;

namespace Bsa.Msa.Common.Services.MessageHandling
{
	/// <summary>
	/// Provides an abstraction for creating message handler instances.
	/// This factory interface enables the creation of both untyped and typed message handlers,
	/// allowing for flexible message processing patterns in messaging architectures.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The IMessageHandlerFactory serves as a central point for instantiating message handlers
	/// with their required dependencies. This abstraction allows for:
	/// - Dependency injection of handler creation logic
	/// - Testing through mock implementations
	/// - Custom handler instantiation strategies (caching, pooling, etc.)
	/// - Different handler implementations based on message type or configuration
	/// </para>
	/// <para>
	/// This factory pattern is particularly useful in scenarios where handlers need to be
	/// created dynamically based on message types, routing information, or runtime configuration.
	/// </para>
	/// </remarks>
	public interface IMessageHandlerFactory
	{
		/// <summary>
		/// Creates a non-generic message handler for processing messages of the specified type.
		/// This method is used when the message type is known but the response type may vary or is not required.
		/// </summary>
		/// <typeparam name="TMessage">The type of message that the handler will process.</typeparam>
		/// <param name="type">A string identifier that specifies the handler type or routing key. 
		/// This can be used to select specific handler implementations when multiple handlers exist for the same message type.</param>
		/// <param name="settings">The configuration settings required by the message handler, 
		/// such as connection strings, timeouts, or handler-specific options.</param>
		/// <param name="simpleBus">The simple bus instance used for message transport operations, 
		/// enabling the handler to publish or send messages to other components.</param>
		/// <param name="localBus">The local bus instance for in-process message communication, 
		/// allowing the handler to interact with other local components without network overhead.</param>
		/// <returns>A fully configured IMessageHandler instance ready to process messages of type TMessage.</returns>
		/// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the specified handler type cannot be created or resolved.</exception>
		/// <example>
		/// <code>
		/// // Create a handler for order processing messages
		/// var handler = factory.Create&lt;OrderMessage&gt;("OrderProcessor", settings, simpleBus, localBus);
		/// await handler.HandleAsync(orderMessage);
		/// </code>
		/// </example>
		IMessageHandler Create<TMessage>(string type, ISettings settings, ISimpleBus simpleBus, ILocalBus localBus);

		/// <summary>
		/// Creates a typed message handler that processes messages and returns responses.
		/// This generic method provides type-safe handling for request-response messaging patterns.
		/// </summary>
		/// <typeparam name="TMessage">The type of message that the handler will receive as input.</typeparam>
		/// <typeparam name="TResponse">The type of response that the handler will return after processing.</typeparam>
		/// <param name="type">A string identifier that specifies the handler type or routing key.
		/// This enables selection of specific handler implementations when multiple handlers exist for the same message type.</param>
		/// <param name="settings">The configuration settings required by the message handler,
		/// including any handler-specific parameters or environment configuration.</param>
		/// <param name="simpleBus">The simple bus instance used for message transport operations,
		/// enabling the handler to publish messages to external systems or services.</param>
		/// <param name="localBus">The local bus instance for in-process message communication,
		/// allowing the handler to communicate with other local components or emit events.</param>
		/// <returns>A fully configured IMessageHandler&lt;TMessage, TResponse&gt; instance capable of 
		/// processing messages and returning typed responses.</returns>
		/// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the specified handler type cannot be created, resolved, 
		/// or when the handler doesn't support the specified response type.</exception>
		/// <example>
		/// <code>
		/// // Create a handler for user authentication that returns an authentication result
		/// var authHandler = factory.Create&lt;AuthRequest, AuthResponse&gt;("Authenticator", settings, simpleBus, localBus);
		/// var response = await authHandler.HandleAsync(loginRequest);
		/// 
		/// // Create a handler for order creation that returns order details
		/// var orderHandler = factory.Create&lt;CreateOrderCommand, OrderResult&gt;("OrderService", settings, simpleBus, localBus);
		/// var orderResult = await orderHandler.HandleAsync(createOrderCommand);
		/// </code>
		/// </example>
		/// <remarks>
		/// This method is typically used for request-response patterns where:
		/// - The caller expects a specific response after message processing
		/// - Type safety is required for both input and output
		/// - The handler implements business logic that produces a result
		/// 
		/// The created handler may support:
		/// - Synchronous or asynchronous processing
		/// - Validation of input messages
		/// - Error handling and retry logic
		/// - Response transformation or enrichment
		/// </remarks>
		IMessageHandler<TMessage, TResponse> Create<TMessage, TResponse>(string type, ISettings settings, ISimpleBus simpleBus, ILocalBus localBus);
	}


}
