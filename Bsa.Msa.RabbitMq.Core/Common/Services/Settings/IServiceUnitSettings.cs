using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.Services.Settings
{
	public interface IServiceUnitSettings : ISettings
	{
		string Type { get; }

		int DegreeOfParallelism { get; }

		string Postfix { get; set; }
	}
}
