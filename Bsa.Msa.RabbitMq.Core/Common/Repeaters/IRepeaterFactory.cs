using Bsa.Msa.Common.Services.Commands;
using System;

namespace Bsa.Msa.Common.Repeaters
{
	public interface IRepeaterFactory
	{
		IRepeater Create(ICommandSettings commandSettings);
	}

	public sealed class RepeaterFactory : IRepeaterFactory
	{
		public IRepeater Create(ICommandSettings commandSettings)
		{
			switch (commandSettings.Mode)
			{
				case RepeaterConcurrentMode.DisallowConcurrentMode:
					return new DisallowConcurrentModeRepeater(commandSettings);
				default:
					return new AllowConcurrenceModeRepeater(commandSettings);
			}
		}
	}



}
