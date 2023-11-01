using System;

namespace Bsa.Msa.Common.Repeaters
{
	public interface IRepeaterFactory
	{
		IRepeater Create(TimeSpan dueTime, TimeSpan period, RepeaterConcurrentMode mode = RepeaterConcurrentMode.DisallowConcurrentMode);
	}

	public sealed class RepeaterFactory : IRepeaterFactory
	{
		public IRepeater Create(TimeSpan dueTime, TimeSpan period, RepeaterConcurrentMode mode = RepeaterConcurrentMode.DisallowConcurrentMode)
		{
			switch (mode)
			{
				case RepeaterConcurrentMode.DisallowConcurrentMode:
					return new DisallowConcurrentModeRepeater(dueTime, period);
				default:
					return new AllowConcurrenceModeRepeater(dueTime, period);
			}
		}
	}



}
