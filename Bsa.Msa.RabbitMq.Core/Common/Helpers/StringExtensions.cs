using System;

namespace Bsa.Msa.Common.Helpers
{
	public static class StringExtensions
	{
		/// <summary>
		/// Создает дату и время в формате UTC из строки, содержащий значение даты и времени в спецификации UNIX.
		/// </summary>
		/// <param name="value">Значение.</param>
		/// <returns>Дата и время.</returns>
		public static DateTime ToUnixUtc(this string value)
		{
			var unix = double.Parse(value);
			return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unix);
		}
	}
}