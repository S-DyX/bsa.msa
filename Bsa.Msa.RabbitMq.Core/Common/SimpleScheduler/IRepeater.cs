using System;

namespace Bsa.Msa.Common.SimpleScheduler
{
	public interface IRepeater : IDisposable
	{
		void Start();
	}
}