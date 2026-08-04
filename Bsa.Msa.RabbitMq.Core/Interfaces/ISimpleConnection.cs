using System;
using RabbitMQ.Client;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	/// <summary>
	/// Represents a simplified connection to a message broker (e.g., RabbitMQ).
	/// Provides methods for configuration, execution, subscription, and lifecycle management.
	/// </summary>
	public interface ISimpleConnection : IDisposable
	{
		/// <summary>
		/// Gets a value indicating whether the underlying connection is currently open and active.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Adds an action to be executed when the connection is established or re-established.
		/// This action is typically used to declare queues, exchanges, or bindings.
		/// </summary>
		/// <param name="action">A delegate that receives a factory function returning an <see cref="IModel"/> channel.</param>
		void Add(Action<Func<IModel>> action);

		/// <summary>
		/// Occurs before a connection attempt is made (initial connection or reconnection).
		/// Can be used to perform cleanup or logging.
		/// </summary>
		event Action BeforeConnect;

		/// <summary>
		/// Occurs after a connection has been successfully established.
		/// Can be used to trigger post‑connection logic.
		/// </summary>
		event Action AfterConnect;

		/// <summary>
		/// Configures a named action that will be executed during subscription or reconnection.
		/// </summary>
		/// <param name="name">A unique name identifying this configuration entry.</param>
		/// <param name="action">The action to execute, receiving a channel factory function.</param>
		/// <param name="ignoreException">If <c>true</c>, exceptions thrown by the action will be swallowed; otherwise, they will propagate.</param>
		void Configure(string name, Action<Func<IModel>> action, bool ignoreException = false);

		/// <summary>
		/// Executes an action immediately on the current connection, using a channel obtained from the factory.
		/// </summary>
		/// <param name="action">The action to perform, receiving a channel factory function.</param>
		/// <param name="name">Optional name for logging or identification purposes.</param>
		void Execute(Action<Func<IModel>> action, string name = null);

		/// <summary>
		/// Subscribes all previously configured actions, causing them to be executed in the order they were added.
		/// Typically called after the connection is established to set up consumers and declarations.
		/// </summary>
		void SubscribeAll();

		/// <summary>
		/// Forces a reconnection to the broker, optionally re‑executing the named configuration actions.
		/// </summary>
		/// <param name="name">
		/// Optional name of a specific configuration to re‑execute after reconnection.
		/// If <c>null</c>, all configurations are re‑executed.
		/// </param>
		void Reconnect(string? name = null);

		/// <summary>
		/// Closes the current connection and releases any associated resources.
		/// </summary>
		void Close();

		/// <summary>
		/// Creates a new channel (<see cref="IModel"/>) for interacting with the broker.
		/// </summary>
		/// <param name="name">Optional name for the channel, used for logging or diagnostics.</param>
		/// <returns>A new <see cref="IModel"/> instance.</returns>
		IModel CreateModel(string name);
	}

}
