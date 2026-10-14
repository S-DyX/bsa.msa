using System;
using System.Collections.Generic;
using System.Linq;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Services.Settings;

namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// Handler and command definitions loaded from a source.
	/// Handlers and commands are identified by their names.
	/// </summary>
	public sealed class ServiceDefinitions
	{
		/// <summary>
		/// No handlers and no commands.
		/// </summary>
		public static readonly ServiceDefinitions Empty = new ServiceDefinitions(null, null);

		/// <summary>
		/// Creates the definitions.
		/// </summary>
		/// <param name="handlers">Message handlers.</param>
		/// <param name="commands">Commands.</param>
		public ServiceDefinitions(
			IEnumerable<MessageHandlerSettings> handlers,
			IEnumerable<CommandSettings> commands)
		{
			Handlers = (handlers ?? Enumerable.Empty<MessageHandlerSettings>())
				.Where(x => x != null)
				.ToList();
			Commands = (commands ?? Enumerable.Empty<CommandSettings>())
				.Where(x => x != null)
				.ToList();
		}

		/// <summary>
		/// Message handlers.
		/// </summary>
		public IReadOnlyList<MessageHandlerSettings> Handlers { get; }

		/// <summary>
		/// Commands.
		/// </summary>
		public IReadOnlyList<CommandSettings> Commands { get; }

		/// <summary>
		/// Collects the handlers and commands of all services.
		/// </summary>
		/// <param name="services">Services, e.g. from services.json.</param>
		/// <returns>The definitions.</returns>
		public static ServiceDefinitions FromServices(IEnumerable<ServiceSettings> services)
		{
			var list = (services ?? Enumerable.Empty<ServiceSettings>()).ToList();
			return new ServiceDefinitions(
				list.SelectMany(x => x.GetHandlers()),
				list.SelectMany(x => x.GetCommands()));
		}

		/// <summary>
		/// Returns a string that changes when any definition changes.
		/// </summary>
		/// <returns>Fingerprint of all definitions.</returns>
		internal string GetFingerprint()
		{
			var handlers = Handlers.Select(SettingsFingerprint.Get);
			var commands = Commands.Select(SettingsFingerprint.Get);
			return string.Join(
				"\n--\n",
				handlers.Concat(commands).OrderBy(x => x, StringComparer.Ordinal));
		}
	}
}
