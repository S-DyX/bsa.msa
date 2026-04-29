using System;
using System.Collections.Generic;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	/// <summary>
	/// Provides a high-level abstraction for managing RabbitMQ message bus operations including sending, publishing,
	/// and responding to messages. This interface serves as the primary entry point for RabbitMQ-based messaging patterns.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The IBusManager interface abstracts RabbitMQ's core concepts (exchanges, queues, bindings, channels)
	/// and provides a simplified API for common messaging patterns:
	/// </para>
	/// <list type="bullet">
	/// <item><description><b>Point-to-Point</b> - Direct message sending to specific queues</description></item>
	/// <item><description><b>Publish/Subscribe</b> - Broadcasting messages to exchanges with routing keys</description></item>
	/// <item><description><b>Request/Response (RPC)</b> - Synchronous communication with correlation IDs</description></item>
	/// <item><description><b>Queue Management</b> - Deleting queues and inspecting messages from exchanges</description></item>
	/// </list>
	/// <para>
	/// This interface inherits IDisposable to ensure proper cleanup of RabbitMQ resources including:
	/// connections, channels, consumers, and temporary reply queues.
	/// </para>
	/// <para>
	/// <b>Thread Safety:</b> Implementations should be thread-safe for concurrent send/publish operations,
	/// but may not be safe for concurrent disposal.
	/// </para>
	/// </remarks>
	public interface IBusManager : IDisposable
	{
		/// <summary>
		/// Sends a message to the default queue configured for the specified message type.
		/// This implements RabbitMQ point-to-point messaging where a single consumer receives the message.
		/// </summary>
		/// <typeparam name="TMessage">The type of message to send. Must be a reference type (class).</typeparam>
		/// <param name="message">The message instance to send. Will be serialized (typically as JSON).</param>
		/// <param name="ttl">ms</param>
		/// <exception cref="ArgumentNullException">Thrown when the message is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when no queue is configured for the message type, the connection is closed, or the channel is unavailable.</exception>
		/// <exception cref="RabbitMQ.Client.Exceptions.OperationInterruptedException">Thrown when the RabbitMQ connection is interrupted during the operation.</exception>
		/// <remarks>
		/// <para>
		/// This method uses the default routing configuration for the message type:
		/// </para>
		/// <list type="bullet">
		/// <item><description>Messages are sent to a queue named using the message type (e.g., "OrderCreated")</description></item>
		/// <item><description>Queue is declared with durable persistence by default</description></item>
		/// <item><description>Messages are published as persistent to survive broker restarts</description></item>
		/// <item><description>Uses a direct exchange with routing key equal to the queue name</description></item>
		/// </list>
		/// <para>
		/// This method is ideal for command messages where exactly one consumer should process the message.
		/// </para>
		/// <example>
		/// <code>
		/// var orderCommand = new CreateOrderCommand { OrderId = 123, Amount = 99.99m };
		/// busManager.Send(orderCommand); // Sent to "CreateOrderCommand" queue
		/// </code>
		/// </example>
		/// </remarks>
		void Send<TMessage>(TMessage message, int? ttl = null) where TMessage : class;

		/// <summary>
		/// Sends a message to a specific RabbitMQ queue, optionally forcing the send even if the queue doesn't exist.
		/// Provides granular control over queue targeting for point-to-point messaging.
		/// </summary>
		/// <typeparam name="TMessage">The type of message to send. Must be a reference type (class).</typeparam>
		/// <param name="queue">The name of the target RabbitMQ queue.</param>
		/// <param name="message">The message instance to send.</param>
		/// <param name="ttl">ms</param>
		/// <param name="forceSend">When true, attempts to send even if the queue hasn't been declared; when false, ensures queue exists before sending.</param>
		/// <exception cref="ArgumentNullException">Thrown when the message or queue name is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the queue name is empty or whitespace.</exception>
		/// <exception cref="InvalidOperationException">Thrown when forceSend is false and the queue doesn't exist, or when the RabbitMQ connection is unavailable.</exception>
		/// <remarks>
		/// <para>
		/// This method provides more explicit queue control compared to the overload without queue parameter:
		/// </para>
		/// <list type="bullet">
		/// <item><description>When forceSend = true, messages are sent without queue declaration (useful for sending to pre-existing queues)</description></item>
		/// <item><description>When forceSend = false, the queue is declared as durable before sending</description></item>
		/// <item><description>Allows sending to temporary or dynamically named queues</description></item>
		/// <item><description>Useful for scenarios where queue names are determined at runtime</description></item>
		/// </list>
		/// <para>
		/// <b>RabbitMQ Implementation Details:</b>
		/// - Uses a direct exchange with the queue name as routing key
		/// - Messages are published as persistent (deliveryMode = 2)
		/// - Content type is typically set to "application/json"
		/// </para>
		/// <example>
		/// <code>
		/// // Send to a tenant-specific queue
		/// busManager.Send($"tenant-{tenantId}-queue", new OrderMessage(), forceSend: true);
		/// 
		/// // Send with automatic queue declaration
		/// busManager.Send("processing.queue", new DocumentMessage(), forceSend: false);
		/// </code>
		/// </example>
		/// </remarks>
		void Send<TMessage>(string queue, TMessage message, int? ttl = null, bool forceSend = false) where TMessage : class;

		/// <summary>
		/// Publishes a message to the default exchange configured for the message type.
		/// Implements RabbitMQ publish/subscribe pattern where multiple consumers can receive the message.
		/// </summary>
		/// <typeparam name="TMessage">The type of message to publish. Must be a reference type (class).</typeparam>
		/// <param name="message">The message instance to publish.</param>
		/// <exception cref="ArgumentNullException">Thrown when the message is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the exchange cannot be declared or the connection is unavailable.</exception>
		/// <remarks>
		/// <para>
		/// This method uses RabbitMQ exchanges to broadcast messages:
		/// </para>
		/// <list type="bullet">
		/// <item><description>Messages are published to a topic exchange named after the message type (e.g., "OrderCreated")</description></item>
		/// <item><description>Consumers bind their queues to this exchange with routing keys</description></item>
		/// <item><description>Multiple consumers can receive the same message</description></item>
		/// <item><description>Messages are persistent and survive broker restarts</description></item>
		/// </list>
		/// <para>
		/// Use this method for domain events, notifications, or any scenario where multiple components need to react to the same message.
		/// </para>
		/// <example>
		/// <code>
		/// var orderEvent = new OrderCreatedEvent { OrderId = 123, CustomerId = 456 };
		/// busManager.Publish(orderEvent); // Published to "OrderCreatedEvent" exchange
		/// // All subscribers to this exchange will receive the message
		/// </code>
		/// </example>
		/// </remarks>
		void Publish<TMessage>(TMessage message) where TMessage : class;

		/// <summary>
		/// Deletes a RabbitMQ queue for the specified message type and queue name combination.
		/// Useful for cleaning up resources when queues are no longer needed.
		/// </summary>
		/// <typeparam name="TMessage">The message type associated with the queue.</typeparam>
		/// <param name="queue">The name of the queue to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when the queue name is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the queue name is empty or whitespace.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the queue cannot be deleted due to active consumers or connection issues.</exception>
		/// <remarks>
		/// <para>
		/// RabbitMQ deletion behavior:
		/// </para>
		/// <list type="bullet">
		/// <item><description>If the queue has active consumers, deletion fails unless force is implemented</description></item>
		/// <item><description>If the queue doesn't exist, the method typically completes without error</description></item>
		/// <item><description>Messages in the queue are permanently deleted</description></item>
		/// <item><description>Bindings to exchanges are automatically removed</description></item>
		/// </list>
		/// <para>
		/// Use with caution in production environments as this permanently removes messages.
		/// </para>
		/// </remarks>
		void Delete<TMessage>(string queue) where TMessage : class;

		/// <summary>
		/// Deletes the default RabbitMQ queue associated with the specified message type.
		/// </summary>
		/// <typeparam name="TMessage">The message type whose default queue should be deleted.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown when the default queue cannot be determined or deletion fails.</exception>
		/// <remarks>
		/// <para>
		/// This method deletes the queue that would normally be used by the Send(TMessage) overload.
		/// The queue name is typically derived from the message type name (e.g., "OrderCommand").
		/// </para>
		/// <para>
		/// <b>Use Cases:</b>
		/// - Reset application state during integration tests
		/// - Clean up after migration scripts
		/// - Remove obsolete queues after message type deprecation
		/// </para>
		/// <example>
		/// <code>
		/// // Delete the default queue for OrderCommand messages
		/// busManager.Delete&lt;OrderCommand&gt;();
		/// </code>
		/// </example>
		/// </remarks>
		void Delete<TMessage>() where TMessage : class;

		/// <summary>
		/// Deletes a RabbitMQ queue by its exact name.
		/// Provides the most direct control over queue deletion.
		/// </summary>
		/// <param name="queue">The exact name of the RabbitMQ queue to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when the queue name is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the queue name is empty or whitespace.</exception>
		/// <remarks>
		/// <para>
		/// This method is useful for:
		/// </para>
		/// <list type="bullet">
		/// <item><description>Deleting queues created with custom names</description></item>
		/// <item><description>Cleaning up temporary or test queues</description></item>
		/// <item><description>Removing orphaned queues after consumer cleanup</description></item>
		/// </list>
		/// <para>
		/// The deletion is permanent and cannot be undone. All messages in the queue will be lost.
		/// </para>
		/// </remarks>
		void Delete(string queue);

		/// <summary>
		/// Publishes a message to a RabbitMQ topic exchange with fine-grained routing control.
		/// Enables complex routing scenarios using topic exchanges and routing keys.
		/// </summary>
		/// <typeparam name="TMessage">The type of message to publish.</typeparam>
		/// <param name="message">The message instance to publish.</param>
		/// <param name="topic">The routing key/topic for message routing (supports wildcards: * for one word, # for multiple words).</param>
		/// <param name="exchangeName">Optional exchange name. If null, a default exchange named after the message type is used.</param>
		/// <exception cref="ArgumentNullException">Thrown when the message is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the topic is null or empty.</exception>
		/// <remarks>
		/// <para>
		/// <b>RabbitMQ Topic Exchange Pattern:</b>
		/// </para>
		/// <list type="bullet">
		/// <item><description><c>*</c> (star) matches exactly one word (e.g., "user.*" matches "user.created" but not "user.created.success")</description></item>
		/// <item><description><c>#</c> (hash) matches zero or more words (e.g., "user.#" matches "user.created", "user.created.success", "user")</description></item>
		/// <item><description>Words are separated by dots (.)</description></item>
		/// </list>
		/// <para>
		/// <b>Common Use Cases:</b>
		/// </para>
		/// <list type="bullet">
		/// <item><description>Multi-tenant routing: $"tenant.{tenantId}.order.created"</description></item>
		/// <item><description>Event categorization: "events.order.created"</description></item>
		/// <item><description>Regional routing: "europe.orders.new"</description></item>
		/// <item><description>Versioned APIs: "v1.user.updated"</description></item>
		/// </list>
		/// <example>
		/// <code>
		/// // Publish with routing key pattern
		/// busManager.Publish(new OrderEvent(), "orders.created", "custom.exchange");
		/// 
		/// // Publish for specific tenant
		/// busManager.Publish(new TenantEvent(), $"tenants.{tenantId}.updated");
		/// 
		/// // Publish to wildcard-enabled exchange
		/// busManager.Publish(new LogEvent(), "logs.critical.*");
		/// </code>
		/// </example>
		/// </remarks>
		void Publish<TMessage>(TMessage message, string topic, string exchangeName = null) where TMessage : class;

		/// <summary>
		/// Retrieves messages from a RabbitMQ queue by consuming from an exchange binding.
		/// This method is typically used for inspection, testing, or administrative purposes.
		/// </summary>
		/// <typeparam name="TMessage">The expected message type to deserialize.</typeparam>
		/// <param name="queueName">The name of the RabbitMQ queue to inspect.</param>
		/// <returns>A list of deserialized messages of type TMessage from the specified queue.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the queueName is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the queue cannot be accessed or messages cannot be deserialized.</exception>
		/// <remarks>
		/// <para>
		/// <b>Important Considerations:</b>
		/// </para>
		/// <list type="bullet">
		/// <item><description>This method consumes and removes messages from the queue</description></item>
		/// <item><description>Primarily intended for testing, debugging, and administrative operations</description></item>
		/// <item><description>Not recommended for production message consumption</description></item>
		/// <item><description>May return fewer messages than actually in the queue</description></item>
		/// <item><description>Messages are automatically acknowledged after retrieval</description></item>
		/// </list>
		/// <para>
		/// Use this method sparingly in production as it can disrupt normal message flow.
		/// </para>
		/// <example>
		/// <code>
		/// // Get up to default count of messages from queue
		/// var messages = busManager.GetMessageExchange&lt;OrderEvent&gt;("order.queue");
		/// 
		/// // Inspect messages for testing
		/// foreach (var msg in messages)
		/// {
		///     Console.WriteLine($"Found message: {msg.OrderId}");
		/// }
		/// </code>
		/// </example>
		/// </remarks>
		List<TMessage> GetMessageExchange<TMessage>(string queueName);

		/// <summary>
		/// Retrieves a specified number of messages from a RabbitMQ queue.
		/// Provides control over how many messages to fetch for inspection or testing purposes.
		/// </summary>
		/// <typeparam name="TMessage">The expected message type to deserialize.</typeparam>
		/// <param name="queueName">The name of the RabbitMQ queue to inspect.</param>
		/// <param name="count">The maximum number of messages to retrieve.</param>
		/// <returns>A list containing up to 'count' deserialized messages of type TMessage.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the queueName is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when count is less than or equal to zero.</exception>
		/// <remarks>
		/// <para>
		/// This overload allows limiting the number of messages retrieved, which is useful for:
		/// </para>
		/// <list type="bullet">
		/// <item><description>Sampling messages for debugging</description></item>
		/// <item><description>Testing without emptying the entire queue</description></item>
		/// <item><description>Performance-constrained inspection scenarios</description></item>
		/// <item><description>Dashboard or monitoring displays</description></item>
		/// </list>
		/// <para>
		/// <b>RabbitMQ Implementation:</b>
		/// Uses basic.get to fetch messages without establishing a consumer, which is
		/// more efficient for inspection than subscribing.
		/// </para>
		/// <example>
		/// <code>
		/// // Get only the first 5 messages
		/// var recentMessages = busManager.GetMessageExchange&lt;OrderEvent&gt;("order.queue", 5);
		/// 
		/// // Check if there are any messages in the queue
		/// var firstMessage = busManager.GetMessageExchange&lt;OrderEvent&gt;("order.queue", 1);
		/// if (firstMessage.Any())
		/// {
		///     // Process the message
		/// }
		/// </code>
		/// </example>
		/// </remarks>
		List<TMessage> GetMessageExchange<TMessage>(string queueName, int count);

		/// <summary>
		/// Sets up a RabbitMQ RPC (Remote Procedure Call) responder that handles requests and returns responses.
		/// Implements the request-response pattern with automatic correlation ID handling.
		/// </summary>
		/// <typeparam name="TRequest">The type of request message expected.</typeparam>
		/// <typeparam name="TResponse">The type of response message to return.</typeparam>
		/// <param name="response">A delegate function that processes the request and returns a response.</param>
		/// <returns>An IDisposable that unregisters the responder when disposed.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the response delegate is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the responder cannot be registered.</exception>
		/// <remarks>
		/// <para>
		/// <b>RabbitMQ RPC Pattern Implementation:</b>
		/// </para>
		/// <list type="bullet">
		/// <item><description>Creates a temporary reply queue for receiving responses</description></item>
		/// <item><description>Handles correlation IDs automatically to match requests with responses</description></item>
		/// <item><description>Manages timeouts for pending requests</description></item>
		/// <item><description>Uses a default request queue named after TRequest type</description></item>
		/// </list>
		/// <para>
		/// <b>Important Notes:</b>
		/// </para>
		/// <list type="bullet">
		/// <item><description>The responder runs on the default request queue (type-based)</description></item>
		/// <item><description>Multiple responders can be registered for the same request type</description></item>
		/// <item><description>Responses are automatically routed back to the caller's reply queue</description></item>
		/// <item><description>Dispose the returned IDisposable to stop responding to requests</description></item>
		/// </list>
		/// <example>
		/// <code>
		/// // Register a calculator service
		/// var responder = busManager.Respond&lt;AddRequest, AddResponse&gt;(request => 
		/// {
		///     return new AddResponse { Result = request.A + request.B };
		/// });
		/// 
		/// // Later, dispose to stop responding
		/// responder.Dispose();
		/// </code>
		/// </example>
		/// </remarks>
		IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response)
			where TRequest : class
			where TResponse : class;

		/// <summary>
		/// Sets up a RabbitMQ RPC responder on a specific queue.
		/// Provides control over which queue the responder listens to for request messages.
		/// </summary>
		/// <typeparam name="TRequest">The type of request message expected.</typeparam>
		/// <typeparam name="TResponse">The type of response message to return.</typeparam>
		/// <param name="response">A delegate function that processes the request and returns a response.</param>
		/// <param name="queueName">The name of the RabbitMQ queue to listen for requests on.</param>
		/// <returns>An IDisposable that unregisters the responder when disposed.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the response delegate or queueName is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the queueName is empty or whitespace.</exception>
		/// <remarks>
		/// <para>
		/// This overload allows for more flexible RPC configurations:
		/// </para>
		/// <list type="bullet">
		/// <item><description>Multiple responders can listen on different queues</description></item>
		/// <item><description>Enables versioned API endpoints (e.g., "v1.calculator", "v2.calculator")</description></item>
		/// <item><description>Allows tenant-isolated responders (e.g., $"calculator.{tenantId}")</description></item>
		/// <item><description>Supports load balancing across multiple responder instances</description></item>
		/// </list>
		/// <para>
		/// <b>Queue Declaration:</b>
		/// The specified queue is declared as durable if it doesn't exist, ensuring
		/// messages are not lost if the responder is temporarily unavailable.
		/// </para>
		/// <para>
		/// <b>Concurrency:</b>
		/// Multiple responders on the same queue will distribute requests using
		/// RabbitMQ's round-robin delivery pattern.
		/// </para>
		/// <example>
		/// <code>
		/// // Versioned API responders
		/// var v1Responder = busManager.Respond&lt;CalculateRequest, CalculateResponse&gt;(
		///     request => CalculateV1(request), "calculator.v1");
		///     
		/// var v2Responder = busManager.Respond&lt;CalculateRequest, CalculateResponse&gt;(
		///     request => CalculateV2(request), "calculator.v2");
		/// 
		/// // Tenant-specific responder
		/// var tenantResponder = busManager.Respond&lt;OrderRequest, OrderResponse&gt;(
		///     request => ProcessOrder(request, tenantId), $"orders.{tenantId}");
		/// 
		/// // Dispose when no longer needed
		/// tenantResponder.Dispose();
		/// </code>
		/// </example>
		/// </remarks>
		IDisposable Respond<TRequest, TResponse>(Func<TRequest, TResponse> response, string queueName)
			where TRequest : class
			where TResponse : class;
	}


}