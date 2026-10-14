using System.Collections.Generic;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// Runtime control of handlers and commands without a restart.
	/// Implemented by <see cref="Impl.ServiceUnitManager"/>.
	/// </summary>
	public interface IServiceUnitController
	{
		/// <summary>
		/// Returns the state of all handlers and commands.
		/// </summary>
		/// <returns>Unit states.</returns>
		IReadOnlyList<ServiceUnitState> GetUnits();

		/// <summary>
		/// Adds a handler or replaces the handler with the same name, and starts it.
		/// </summary>
		/// <param name="settings">Handler settings.</param>
		void AddHandler(MessageHandlerSettings settings);

		/// <summary>
		/// Starts a stopped handler.
		/// </summary>
		/// <param name="name">Handler name.</param>
		/// <returns><c>false</c> if there is no such handler or it can not run.</returns>
		bool StartHandler(string name);

		/// <summary>
		/// Stops a handler: it stops reading its queue, the messages stay in the queue.
		/// </summary>
		/// <param name="name">Handler name.</param>
		/// <returns><c>false</c> if there is no such handler.</returns>
		bool StopHandler(string name);

		/// <summary>
		/// Stops and forgets a handler.
		/// </summary>
		/// <param name="name">Handler name.</param>
		/// <returns><c>false</c> if there is no such handler.</returns>
		bool RemoveHandler(string name);

		/// <summary>
		/// Adds a command or replaces the command with the same name, and starts it.
		/// </summary>
		/// <param name="settings">Command settings.</param>
		void AddCommand(CommandSettings settings);

		/// <summary>
		/// Starts a stopped command.
		/// </summary>
		/// <param name="name">Command name.</param>
		/// <returns><c>false</c> if there is no such command.</returns>
		bool StartCommand(string name);

		/// <summary>
		/// Stops running a command by its schedule.
		/// </summary>
		/// <param name="name">Command name.</param>
		/// <returns><c>false</c> if there is no such command.</returns>
		bool StopCommand(string name);

		/// <summary>
		/// Stops and forgets a command.
		/// </summary>
		/// <param name="name">Command name.</param>
		/// <returns><c>false</c> if there is no such command.</returns>
		bool RemoveCommand(string name);

		/// <summary>
		/// Loads the definitions from the source again: starts new units,
		/// stops removed ones and restarts changed ones.
		/// </summary>
		void Reload();
	}
}
