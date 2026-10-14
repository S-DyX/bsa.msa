using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes
{
	/// <summary>
	/// Builds configuration sections from key-value pairs.
	/// </summary>
	public static class ConfigurationFactory
	{
		private const string SectionName = "section";

		/// <summary>
		/// Creates a section with the given values.
		/// </summary>
		/// <param name="values">Section values by key.</param>
		/// <returns>The section.</returns>
		public static IConfigurationSection CreateSection(
			IDictionary<string, string> values)
		{
			var data = new Dictionary<string, string>();
			foreach (var pair in values)
				data[$"{SectionName}:{pair.Key}"] = pair.Value;

			return new ConfigurationBuilder()
				.AddInMemoryCollection(data)
				.Build()
				.GetSection(SectionName);
		}
	}
}
