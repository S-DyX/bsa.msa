using System.Threading.Tasks;

namespace Bsa.Msa.Common.SimpleScheduler
{
	public interface IRepetitiveCommand
	{
		Task ExecuteAsync();
	}
}