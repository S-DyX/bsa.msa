using System;
using System.Linq;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// Detects changes of handler and command settings.
	/// </summary>
	internal static class SettingsFingerprint
	{
		/// <summary>
		/// Returns all configuration values of the settings as one string.
		/// </summary>
		/// <param name="settings">Handler or command settings.</param>
		/// <returns>Fingerprint; equal settings give equal strings.</returns>
		public static string Get(ISettings settings)
		{
			if (settings?.Raw == null) return string.Empty;

			var values = settings.Raw
				.AsEnumerable(makePathsRelative: true)
				.Where(x => x.Value != null)
				.OrderBy(x => x.Key, StringComparer.Ordinal)
				.Select(x => $"{x.Key}={x.Value}");
			var result = string.Join("\n", values);

			// The endpoint also depends on the service postfix, which is not in Raw.
			if (settings is IMessageHandlerSettings handler)
				result += $"\n@endpoint={handler.SubscriptionEndpoint}";

			return result;
		}
	}
}
