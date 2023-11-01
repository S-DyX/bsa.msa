using System.Xml.Linq;
using Bsa.Msa.Common.Settings;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.Common.Services.MessageHandling
{
	public sealed class SubscribeSettings : SettingsConfigurationSection, ISubscribeSettings
	{
		public string SubscriptionEndpoint { get; private set; }

		public string RoutingKey { get; private set; }

		public string Type { get; private set; }

		public string Name { get; private set; }


		public SubscribeSettings(IConfigurationSection raw)
			: base(raw)
		{
			Name = GetAttValue(raw, "name");
			Type = GetAttValue(raw, "type");
			RoutingKey = GetAttValue(raw, "routingKey");
			SubscriptionEndpoint = GetAttValue(raw, "subscriptionEndpoint");
		}
	}
}