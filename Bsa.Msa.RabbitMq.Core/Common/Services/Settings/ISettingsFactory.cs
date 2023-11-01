using System.Xml.Linq;
using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.Services.Settings
{
	public interface ISettingsFactory
	{
		TSettings Create<TSettings>(XElement raw) where TSettings : ISettings;

		TSettings Create<TSettings>() where TSettings : ISettings;
	}
}
