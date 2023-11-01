using System;
using System.Diagnostics;

namespace Bsa.Msa.Common
{
	public class StopWatchProxy : IDisposable
	{
		private readonly Stopwatch _stopwatch;
		public StopWatchProxy()
		{
			_stopwatch = Stopwatch.StartNew();
		}

		public TimeSpan Elapsed => _stopwatch.Elapsed;

		public void Dispose()
		{
			_stopwatch.Stop();
		}
	}
}
