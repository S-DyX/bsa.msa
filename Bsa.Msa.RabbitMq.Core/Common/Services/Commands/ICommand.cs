using System;

namespace Bsa.Msa.Common.Services.Commands
{
	public interface ICommand : IDisposable
	{
		void Execute();
	}
}
