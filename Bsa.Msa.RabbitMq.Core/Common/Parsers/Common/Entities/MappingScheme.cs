using System.Collections.Generic;

namespace Bsa.Msa.Common.Parsers.Common.Entities
{
	public class MappingScheme
	{
		public List<KeyValue> Matches { get; set; }

		public List<KeyValue> CleanMatches { get; set; }
	}
}