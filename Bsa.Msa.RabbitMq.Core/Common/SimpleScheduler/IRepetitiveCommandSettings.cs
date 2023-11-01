using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.SimpleScheduler
{
	public interface IRepetitiveCommandSettings : ISettings
	{
		double IntervalInSeconds { get; }
	}
}