using System;

namespace Bsa.Msa.Common.Services.Interfaces
{
	/// <summary>
	/// Singe service unit command or handler(subscriber)
	/// </summary>
	public interface IServiceUnit
	{
		/// <summary>
		/// Start service
		/// </summary>
		void Start();

		/// <summary>
		/// Start async
		/// </summary>
		void StartAsync();

		/// <summary>
		/// Stop
		/// </summary>
		void Stop();

		/// <summary>
		/// <see cref="UnhandledExceptionEventHandler"/>
		/// </summary>
		event UnhandledExceptionEventHandler OnError;

		/// <summary>
		/// Is started
		/// </summary>
		bool IsStarted { get; }

		/// <summary>
		/// Name of service
		/// </summary>
		string Name { get; }
	}
}
