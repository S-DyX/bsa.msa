using Bsa.Msa.Common.Settings;
using System.Collections.Generic;

namespace Bsa.Msa.Common.Services.Settings
{
	/// <summary>
	/// Настройки логических служб
	/// </summary>
	public interface IServicesSettings : ISettings
	{
		/// <summary>
		/// Метод возвращает список служб
		/// </summary>
		/// <returns></returns>
		IEnumerable<ServiceSettings> GetServices();
	}
}
