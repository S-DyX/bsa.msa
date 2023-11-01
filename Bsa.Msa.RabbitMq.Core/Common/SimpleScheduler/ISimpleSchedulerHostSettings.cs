using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.SimpleScheduler
{
	public interface ISimpleSchedulerHostSettings : ISettings
	{
		IRepetitiveCommandSettings[] RepetitiveCommands { get; }
	}
}