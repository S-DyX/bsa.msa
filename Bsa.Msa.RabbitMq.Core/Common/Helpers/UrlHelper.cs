namespace Bsa.Msa.Common.Helpers
{
	public static class UrlHelper
	{
		public static string GetNormalizedUrl(string url)
		{
			var tolowerUrl = url.ToLower();
			if (tolowerUrl.StartsWith("http://"))
			{
				tolowerUrl = tolowerUrl.Remove(0, 7);
			}
			if (tolowerUrl.StartsWith("https://"))
			{
				tolowerUrl = tolowerUrl.Remove(0, 8);
			}
			if (tolowerUrl.StartsWith("www."))
			{
				tolowerUrl = tolowerUrl.Remove(0, 4);
			}
			if (tolowerUrl.EndsWith("/"))
			{
				tolowerUrl = tolowerUrl.Remove(tolowerUrl.Length - 1, 1);
			}
			return tolowerUrl;
		}

		public static string AsNormalizeUrl(this string url)
		{
			return $"http://{GetNormalizedUrl(url)}";
		}

		public static string AsUrlHash(this string url)
		{
			return GetNormalizedUrl(url).ComputeMd5String();
		}

		public static string GetHttpUrl(string url)
		{
			var tolowerUrl = url.ToLower();
			if (tolowerUrl.StartsWith("http://"))
			{
				return url;
			}
			if (tolowerUrl.StartsWith("https://"))
			{
				return url;
			}
			if (tolowerUrl.StartsWith("www."))
			{
				return "http://" + url;
			}
			return "http://" + url;
		}
	}
}
