using System;

namespace Bsa.Msa.Common.Services.Interfaces
{
	/// <summary>
	/// Управление логическими сервисами
	/// </summary>
	public interface IServiceUnitManager
	{
		void Start();

		void Stop();

		void Paused();

		void Continued();


		event UnhandledExceptionEventHandler OnError;
	}
}
