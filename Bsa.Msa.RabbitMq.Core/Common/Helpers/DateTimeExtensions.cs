using System;
using System.Text.RegularExpressions;

namespace Bsa.Msa.Common.Helpers
{
	public static class DateTimeExtensions
	{
		public static double ToUnixTimestamp(this DateTime value)
		{
			return (value - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
		}

		public static int ToUnixDateTime(this DateTime date)
		{
			return (Int32)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
		}

		public static DateTime MinPosix
		{
			get
			{
				return new DateTime(1970, 1, 1);
			}
		}


		public static DateTime ToPosixDateTime(this DateTime date)
		{
			return date < MinPosix ? MinPosix : date;
		}

		public static DateTime ToPosixDateTime(this DateTime? date)
		{
			return !date.HasValue ? MinPosix : ToPosixDateTime(date.Value);
		}

		public static int ToUnixDateTime(this DateTime? date)
		{
			return date.HasValue ? date.Value.ToUnixDateTime() : 0;
		}

		public static DateTime? ToDateTime(this int unixDate)
		{
			if (unixDate <= 0)
				return null;

			return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
				.AddSeconds(unixDate)
				.ToLocalTime();
		}

		public static bool TryParseDateTime(this string dateTime, out DateTime date)
		{
			dateTime = dateTime.ToLower();
			if (dateTime.Contains("сегодня"))
			{
				dateTime = dateTime.Replace("сегодня", DateTime.Today.ToString("dd.MM.yyyy"));
			}
			else if (dateTime.Contains("вчера"))
			{
				dateTime = dateTime.Replace("вчера", DateTime.Today.Subtract(TimeSpan.FromDays(1)).ToString("dd.MM.yyyy"));
			}
			else
			{
				var regexp = new Regex("([0-9]+ )?(минуту|минуты|минут|час|часа|часов|день|дня|дней|неделю|неделя|недели|месяц|месяца|месяцев)( назад)?");
				var result = regexp.Match(dateTime);
				var count = 1;
				var dt = DateTime.Today;
				if (result.Groups[1].Value != string.Empty)
				{
					count = Convert.ToInt32(result.Groups[1].Value);
				}
				switch (result.Groups[2].Value)
				{
					case "минуту":
					case "минуты":
					case "минут":
						dt = dt.Subtract(TimeSpan.FromMinutes(count));
						break;
					case "час":
					case "часа":
					case "часов":
						dt = dt.Subtract(TimeSpan.FromHours(count));
						break;
					case "день":
					case "дня":
					case "дней":
						dt = dt.Subtract(TimeSpan.FromDays(count));
						break;
					case "неделю":
					case "неделя":
					case "недели":
						dt = dt.Subtract(TimeSpan.FromDays(count * 7));
						break;
					case "месяц":
					case "месяца":
					case "месяцев":
						dt = dt.Subtract(TimeSpan.FromDays(count * 30));
						break;
				}
				if (result.Success)
				{
					dateTime = dateTime.Replace(result.Value, dt.ToString("dd.MM.yyyy"));
				}
			}

			return DateTime.TryParse(dateTime, out date);
		}
	}
}