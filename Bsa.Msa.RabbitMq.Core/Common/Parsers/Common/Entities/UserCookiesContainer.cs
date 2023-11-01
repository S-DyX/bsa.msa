using System.Net;

namespace Bsa.Msa.Common.Parsers.Common.Entities
{
	public class UserCookiesContainer
	{
		public string Login { get; set; }

		public string Password { get; set; }

		public CookieCollection Cookies { get; set; }
	}
}
