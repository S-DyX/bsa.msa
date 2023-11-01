using System.Collections.Generic;
using System.Linq;

namespace Bsa.Msa.Common.Helpers
{
	public static class QueryFormatHelper
	{
		public static string GetIdsString(IEnumerable<string> ids)
		{
			return string.Join(",", ids.Select(x => $"'{x}'"));
		}



		public static string GetIdsInt(IEnumerable<int> ids)
		{
			return string.Join(",", ids.Select(x => $"{x}"));
		}

		public static string GetIdsDigit<T>(IEnumerable<T> ids)
		{
			return string.Join(",", ids.Select(x => $"{x}"));
		}

		public static string GetJoinHostСonditions(string onField, int? sourcetypes, int? sourceLevels)
		{
			var conditions = new List<string>();

			if (sourcetypes.HasValue && sourcetypes.Value != 0)
			{
				conditions.Add($"(h.SourceType & {sourcetypes.Value} ) <> 0");
			}

			if (sourceLevels.HasValue && sourceLevels.Value != 0)
			{
				conditions.Add($"(h.SourceLevel & {sourceLevels.Value} ) <> 0");
			}

			if (conditions.Count == 0)
			{
				return string.Empty;
			}

			var result = $" INNER JOIN [dbo].[Hosts] h with(nolock) on h.Id={onField} ";
			return conditions.Aggregate(result, (current, condition) => current + $" AND {condition} ");
		}


	}
}
