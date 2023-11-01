using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.SimpleScheduler
{
	public interface IRepetitiveCommandFactory
	{
		IRepetitiveCommand Create(ISettings settings);

		void Release(IRepetitiveCommand instance);
	}
}