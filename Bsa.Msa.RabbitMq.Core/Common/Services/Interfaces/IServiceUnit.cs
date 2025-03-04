using System;

namespace Bsa.Msa.Common.Services.Interfaces
{
	public interface IServiceUnit
	{
		void Start();

		void StartAsync();

		void Stop();

		event UnhandledExceptionEventHandler OnError;

		bool IsStarted { get; }
	}
}
