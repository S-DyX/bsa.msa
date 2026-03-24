using Bsa.Msa.Common.Services.Commands;
using System;

namespace Bsa.Msa.Common.Repeaters
{
	public interface IRepeaterFactory
	{
		IRepeater Create(ICommandSettings commandSettings, ILocalLogger logger = null);
	}

	public sealed class RepeaterFactory : IRepeaterFactory
	{
		public IRepeater Create(ICommandSettings commandSettings, ILocalLogger logger = null)
		{
			switch (commandSettings.Mode)
			{
				case RepeaterConcurrentMode.DisallowConcurrentMode:
					return new DisallowConcurrentModeRepeater(commandSettings, logger);
				default:
					return new AllowConcurrenceModeRepeater(commandSettings, logger);
			}
		}
	}



}
