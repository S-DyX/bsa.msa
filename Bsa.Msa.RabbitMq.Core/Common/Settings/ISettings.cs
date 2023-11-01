using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

namespace Bsa.Msa.Common.Settings
{
	/// <summary>
	/// Base settings interface
	/// </summary>
	public interface ISettings
	{
		/// <summary>
		/// Raw XML of the settings.
		/// </summary>
		IConfigurationSection Raw { get; }

	}

}
