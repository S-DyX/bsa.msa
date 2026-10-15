namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// State of a handler or a command.
	/// </summary>
	public sealed class ServiceUnitState
	{
		/// <summary>
		/// Creates the state.
		/// </summary>
		/// <param name="name">Unit name.</param>
		/// <param name="kind">Unit kind.</param>
		/// <param name="isEnabled">Whether the unit should run.</param>
		/// <param name="isStarted">Whether the unit is running now.</param>
		/// <param name="subscriptionEndpoint">Queue of a handler; <c>null</c> for a command.</param>
		public ServiceUnitState(
			string name,
			ServiceUnitKind kind,
			bool isEnabled,
			bool isStarted,
			string subscriptionEndpoint = null)
		{
			Name = name;
			Kind = kind;
			IsEnabled = isEnabled;
			IsStarted = isStarted;
			SubscriptionEndpoint = subscriptionEndpoint;
		}

		/// <summary>
		/// Queue of a handler, e.g. for <c>AmqpQueueMonitor</c>; <c>null</c> for a command.
		/// </summary>
		public string SubscriptionEndpoint { get; }

		/// <summary>
		/// Unit name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Unit kind.
		/// </summary>
		public ServiceUnitKind Kind { get; }

		/// <summary>
		/// The unit should run: false after Stop or for a handler
		/// with degreeOfParallelism 0.
		/// </summary>
		public bool IsEnabled { get; }

		/// <summary>
		/// The unit is running now (a handler is connected to the queue).
		/// </summary>
		public bool IsStarted { get; }
	}
}
