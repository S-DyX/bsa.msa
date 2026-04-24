using System;

namespace Bsa.Msa.Common.Services.Interfaces
{
	/// <summary>
	/// Управление логическими сервисами
	/// </summary>
	public interface IServiceUnitManager
	{
		/// <summary>
		/// Start all handlers and command
		/// </summary>
		void Start();

		/// <summary>
		/// Stop all handlers and commands
		/// </summary>
		void Stop();

		/// <summary>
		/// Paused
		/// </summary>
		void Paused();

		/// <summary>
		/// Continued
		/// </summary>
		void Continued();

		/// <summary>
		/// <see cref="UnhandledExceptionEventHandler"/>
		/// </summary>
		event UnhandledExceptionEventHandler OnError;
	}
}
