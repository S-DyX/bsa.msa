using System;
using System.Threading;

namespace Bsa.Msa.Common.Repeaters
{
	public interface IRepeater : IDisposable
	{
	
		void Start(Action<CancellationToken> onRepeat);

		event UnhandledExceptionEventHandler Error;
	}
}
