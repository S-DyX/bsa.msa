using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.SimpleScheduler
{
	public interface IRepeaterRegistryFactory
	{
		IRepeaterRegistry Create(ISettings settings);
	}
}