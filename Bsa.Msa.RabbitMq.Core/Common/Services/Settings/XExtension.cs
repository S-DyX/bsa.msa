using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bsa.Msa.Common.Services.Settings
{
	public static class XExtension
	{
		public static string GetRecursionAttribute(this XElement raw, string attName)
		{
			if (raw == null)
				return string.Empty;

			var att = raw.Attribute("postfix");

			if (string.IsNullOrEmpty(att?.Value))
			{
				return GetRecursionAttribute(raw.Parent, attName);
			}
			return att.Value;
		}
	}
}
