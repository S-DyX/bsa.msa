using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Services.Settings;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// Builds handler and command settings from values of a third-party
	/// storage. The keys are the same as in services.json
	/// (name, type, subscriptionEndpoint, schedule, period...).
	/// </summary>
	public static class ServiceDefinitionFactory
	{
		private const string ItemSection = "item";

		private const string ServicesSection = "services";

		/// <summary>
		/// Creates message handler settings.
		/// </summary>
		/// <param name="values">Settings by key; "name" is required.</param>
		/// <param name="postfix">Endpoint postfix of the service, if any.</param>
		/// <returns>Handler settings.</returns>
		public static MessageHandlerSettings CreateHandler(
			IDictionary<string, string> values,
			string postfix = null)
		{
			var section = ToSection(values);
			return new MessageHandlerSettings(GetName(values), postfix, section);
		}

		/// <summary>
		/// Creates command settings.
		/// </summary>
		/// <param name="values">Settings by key; "name" is required.</param>
		/// <returns>Command settings.</returns>
		public static CommandSettings CreateCommand(IDictionary<string, string> values)
		{
			var section = ToSection(values);
			return new CommandSettings(GetName(values), section);
		}

		/// <summary>
		/// Parses definitions in the services.json format,
		/// e.g. a JSON document stored in a database.
		/// </summary>
		/// <param name="json">JSON with the "services" array.</param>
		/// <returns>The definitions.</returns>
		public static ServiceDefinitions FromJson(string json)
		{
			ArgumentNullException.ThrowIfNull(json);

			using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
			var root = new ConfigurationBuilder().AddJsonStream(stream).Build();
			var services = root.GetSection(ServicesSection)
				.GetChildren()
				.Select(x => new { Name = x.GetSection("name").Value, Section = x })
				.Where(x => !string.IsNullOrEmpty(x.Name))
				.Select(x => new ServiceSettings(x.Name, x.Section))
				.ToList();
			return ServiceDefinitions.FromServices(services);
		}

		private static string GetName(IDictionary<string, string> values)
		{
			if (!values.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The 'name' value is required.", nameof(values));

			return name;
		}

		private static IConfigurationSection ToSection(IDictionary<string, string> values)
		{
			ArgumentNullException.ThrowIfNull(values);

			var data = values.ToDictionary(x => $"{ItemSection}:{x.Key}", x => x.Value);
			return new ConfigurationBuilder()
				.AddInMemoryCollection(data)
				.Build()
				.GetSection(ItemSection);
		}
	}
}
