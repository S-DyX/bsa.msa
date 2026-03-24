
using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.Services.MessageHandling
{
	/// <summary>
	/// Provides configuration settings for RabbitMQ message handlers.
	/// This interface defines the core settings required to configure message consumption,
	/// queue management, and processing behavior for RabbitMQ-based messaging infrastructure.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This interface extends ISettings and provides RabbitMQ-specific configuration options
	/// for controlling message handling behavior, including queue binding, retry policies,
	/// concurrency, and message expiration.
	/// </para>
	/// <para>
	/// Key configuration areas include:
	/// - Queue and exchange binding configuration
	/// - Message processing concurrency and prefetch settings
	/// - Error handling and retry mechanisms
	/// - Queue lifecycle management (TTL, auto-delete, purging)
	/// </para>
	/// </remarks>
	public interface IMessageHandlerSettings : ISettings
	{
		/// <summary>
		/// Gets the subscription endpoint name (typically the queue name) where messages will be consumed.
		/// This defines the queue that the message handler will bind to and receive messages from.
		/// </summary>
		/// <returns>The name of the queue or subscription endpoint to consume messages from.</returns>
		/// <remarks>
		/// This value is used to:
		/// - Declare or bind to a specific queue in RabbitMQ
		/// - Determine the consumer endpoint for message processing
		/// - Route messages when combined with routing keys and exchanges
		/// </remarks>
		string SubscriptionEndpoint { get; }

		/// <summary>
		/// Gets the routing key used for binding queues to exchanges.
		/// The routing key determines which messages will be routed from an exchange to the bound queue.
		/// </summary>
		/// <returns>The routing key pattern for message routing.</returns>
		/// <remarks>
		/// <para>
		/// Routing keys can be:
		/// - Simple strings for direct exchanges: "user.created"
		/// - Patterns for topic exchanges: "user.*" or "user.#"
		/// - Empty strings for fanout exchanges
		/// </para>
		/// <para>
		/// This setting works in conjunction with UseExchange to determine the binding behavior.
		/// </para>
		/// </remarks>
		string RoutingKey { get; }

		/// <summary>
		/// Gets the message handler type identifier.
		/// This string is used to distinguish between different handler implementations
		/// and can be used for dynamic handler selection or routing purposes.
		/// </summary>
		/// <returns>A string identifier representing the handler type.</returns>
		/// <remarks>
		/// Common uses include:
		/// - Handler registration and lookup
		/// - Message type discrimination
		/// - Configuration-based handler selection
		/// - Logging and monitoring identification
		/// </remarks>
		string Type { get; }

		/// <summary>
		/// Gets a value indicating whether messages should be published to an exchange.
		/// When true, the message handler will use exchange-based routing; when false,
		/// it will use direct queue operations.
		/// </summary>
		/// <returns>true if exchange-based messaging is enabled; otherwise, false.</returns>
		/// <remarks>
		/// <para>
		/// Exchange-based messaging enables:
		/// - Pub/sub patterns with multiple consumers
		/// - Topic-based routing with wildcards
		/// - More complex routing scenarios
		/// </para>
		/// <para>
		/// When disabled, the handler works directly with queues, which is simpler
		/// but less flexible for distributed scenarios.
		/// </para>
		/// </remarks>
		bool UseExchange { get; }

		/// <summary>
		/// Gets a value indicating whether message processing retries are enabled.
		/// When enabled, failed messages will be automatically retried based on the retry count configuration.
		/// </summary>
		/// <returns>true if retry logic is enabled; otherwise, false.</returns>
		/// <remarks>
		/// Retry mechanisms typically involve:
		/// - Delayed retry queues (using dead letter exchanges)
		/// - Configurable retry intervals
		/// - Poison message handling after retry exhaustion
		/// </remarks>
		bool Retry { get; }

		/// <summary>
		/// Gets the maximum number of retry attempts for failed messages.
		/// This value is only applicable when Retry is set to true.
		/// </summary>
		/// <returns>The maximum number of retry attempts, or null if no retry count is specified.</returns>
		/// <remarks>
		/// <para>
		/// After exhausting retry attempts, messages are typically:
		/// - Sent to a dead letter queue for manual inspection
		/// - Logged as failed and discarded
		/// - Moved to an error handling queue
		/// </para>
		/// <para>
		/// A null value may indicate that the default retry count should be used
		/// or that retry is disabled regardless of the Retry property.
		/// </para>
		/// </remarks>
		int? RetryCount { get; }

		/// <summary>
		/// Gets the number of messages that can be prefetched from RabbitMQ.
		/// This controls the maximum number of unacknowledged messages that can be
		/// delivered to the consumer at once.
		/// </summary>
		/// <returns>The prefetch count value (typically between 1 and 65535).</returns>
		/// <remarks>
		/// <para>
		/// Prefetch count tuning considerations:
		/// - Higher values increase throughput but may cause message distribution imbalances
		/// - Lower values provide fairer distribution across consumers
		/// - Value of 1 ensures round-robin distribution
		/// - Should be balanced with processing time and concurrency settings
		/// </para>
		/// <para>
		/// Common patterns:
		/// - CPU-bound tasks: lower prefetch (1-10)
		/// - I/O-bound tasks: higher prefetch (50-200)
		/// - Batch processing: prefetch equal to batch size
		/// </para>
		/// </remarks>
		ushort PrefetchCount { get; }

		/// <summary>
		/// Gets the number of parallel tasks or threads that can process messages simultaneously.
		/// This controls the concurrency level for message consumption within a single consumer instance.
		/// </summary>
		/// <returns>The degree of parallelism (number of concurrent message processors).</returns>
		/// <remarks>
		/// <para>
		/// This setting determines:
		/// - Number of concurrent message processing tasks
		/// - Maximum throughput for the consumer
		/// - Resource utilization (CPU, memory, connections)
		/// </para>
		/// <para>
		/// Best practices:
		/// - Set based on available CPU cores and processing characteristics
		/// - Consider database connection pool sizes
		/// - Balance with prefetch count to avoid overloading
		/// - Monitor for memory pressure when increasing parallelism
		/// </para>
		/// </remarks>
		int DegreeOfParallelism { get; }

		/// <summary>
		/// Sets the subscription endpoint name (queue name) for the message handler.
		/// This method allows dynamic configuration of the queue name at runtime.
		/// </summary>
		/// <param name="subscriptionEndpoint">The name of the queue or subscription endpoint to set.</param>
		/// <remarks>
		/// This method is useful for:
		/// - Dynamic queue naming based on runtime parameters
		/// - Multi-tenant scenarios where each tenant has a separate queue
		/// - Testing scenarios with isolated queues
		/// - Overriding default naming conventions
		/// </remarks>
		void SetSubscriptionEndpoint(string subscriptionEndpoint);

		/// <summary>
		/// Gets the time-to-live (TTL) for messages in milliseconds.
		/// Messages that exceed this TTL will expire and may be moved to a dead letter exchange.
		/// </summary>
		/// <returns>The TTL value in milliseconds, or null if no TTL is set.</returns>
		/// <remarks>
		/// <para>
		/// TTL functionality:
		/// - Can be set at the queue level (affects all messages)
		/// - Can be set at the message level (per-message expiration)
		/// - Expired messages can be routed to dead letter exchanges
		/// - Useful for time-sensitive message processing
		/// </para>
		/// <para>
		/// Common use cases:
		/// - Session timeouts
		/// - Time-limited offers
		/// - Cache invalidation
		/// - Delayed processing scenarios
		/// </para>
		/// </remarks>
		int? Ttl { get; }

		/// <summary>
		/// Gets a value indicating whether the queue should be purged (cleared) after the handler starts.
		/// When enabled, all existing messages in the queue will be removed upon consumer startup.
		/// </summary>
		/// <returns>true if the queue should be cleared on startup; otherwise, false.</returns>
		/// <remarks>
		/// <para>
		/// Warning: Enabling this option will permanently delete all pending messages in the queue.
		/// Use with caution, especially in production environments.
		/// </para>
		/// <para>
		/// This is useful for:
		/// - Development and testing scenarios
		/// - Resetting state for integration tests
		/// - Cleaning up stale messages after configuration changes
		/// - Preventing processing of obsolete messages after deployment
		/// </para>
		/// </remarks>
		bool ClearAfterStart { get; }

		/// <summary>
		/// Gets a value indicating whether the queue should be automatically deleted when the last consumer unsubscribes.
		/// This helps with resource cleanup in dynamic or temporary queue scenarios.
		/// </summary>
		/// <returns>true if the queue should auto-delete; otherwise, false.</returns>
		/// <remarks>
		/// <para>
		/// Auto-delete queues are useful for:
		/// - Temporary queues for request-response patterns
		/// - Dynamic consumer scenarios
		/// - Development and testing environments
		/// - Reducing resource waste from orphaned queues
		/// </para>
		/// <para>
		/// Note: Auto-delete queues are removed when the last consumer cancels or disconnects,
		/// not immediately when the consumer stops.
		/// </para>
		/// </remarks>
		bool AutoDelete { get; }

		/// <summary>
		/// Gets a value indicating whether a GUID should be appended to the queue name.
		/// This ensures unique queue names across multiple instances or deployments.
		/// </summary>
		/// <returns>true if a GUID should be appended to the queue name; otherwise, false.</returns>
		/// <remarks>
		/// <para>
		/// Appending GUIDs is particularly useful for:
		/// - Ensuring unique queue names for each consumer instance
		/// - Multi-instance deployments without shared queues
		/// - Avoiding naming conflicts in clustered environments
		/// - Creating ephemeral queues for specific consumers
		/// </para>
		/// <para>
		/// Example queue names:
		/// - Without GUID: "order.processor"
		/// - With GUID: "order.processor.a3f7b8c2-9e4d-4f1a-b3c5-d6e7f8g9h0i1"
		/// </para>
		/// </remarks>
		bool AppendGuid { get; }

		/// <summary>
		/// Gets a value indicating whether the internal queue should be turned off.
		/// This setting allows bypassing the default internal queuing mechanism for special scenarios.
		/// </summary>
		/// <returns>true if the internal queue should be disabled; otherwise, false.</returns>
		/// <remarks>
		/// <para>
		/// Turning off the internal queue might be used for:
		/// - Direct message processing without queuing
		/// - Synchronous processing scenarios
		/// - Performance optimization when queuing is unnecessary
		/// - Custom queuing implementations
		/// </para>
		/// <para>
		/// Warning: Disabling internal queues may affect:
		/// - Message durability guarantees
		/// - Backpressure handling
		/// - Load balancing capabilities
		/// - Failure recovery scenarios
		/// </para>
		/// </remarks>
		bool TurnOffInternalQueue { get; }
	}

}
