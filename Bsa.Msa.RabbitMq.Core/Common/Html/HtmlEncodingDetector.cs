using System.Text.RegularExpressions;

namespace Bsa.Msa.Common.Html
{
	public sealed class HtmlEncodingDetector : IHtmlEncodingDetector
	{
		private static readonly Regex _regexToDetectEncoding = new Regex("<meta(.*?)charset=\"?(?'charset'[^\"]+)\"", RegexOptions.Compiled);

		public string Detect(string htmlContent)
		{
			var match = _regexToDetectEncoding.Match(htmlContent);

			return match.Groups["charset"].Value;
		}
	}
}