using System;
using System.Threading.Tasks;

namespace Bsa.Msa.Common.Services.Interfaces
{
	public interface IServiceUnit
	{
		void Start();

		void StartAsync();

		void Stop();

		event UnhandledExceptionEventHandler OnError;
	}
}
