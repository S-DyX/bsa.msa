using System.Xml.Linq;
using Bsa.Msa.Common.Services.Settings;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.Common.Services.MessageHandling.Entities.Ver001
{
	public class SimpleMessageHandlerSettigns : MessageHandlerSettings, ISimpleMessageHandlerSettigns
	{
		public SimpleMessageHandlerSettigns()
			: base(null)
		{
		}
		public SimpleMessageHandlerSettigns(IConfigurationSection raw)
			: base(raw)
		{
		}
		public SimpleMessageHandlerSettigns(string name, IConfigurationSection raw)
			: base(name, raw)
		{
		}
		public SimpleMessageHandlerSettigns(string name, string postfix, IConfigurationSection raw)
			: base(name, postfix, raw)
		{
		}

		public string PublicationEndpoint { get; private set; }

		public string PublicationEventEndpoint { get; private set; }

		
		public bool DoNotPublish { get; private set; }


		protected override void LoadSettings(IConfigurationSection raw)
		{
			base.LoadSettings(raw);
			SubscriptionEndpoint = GetAttValue(raw, "subscriptionEndpoint");
			//Postfix = raw.GetRecursionAttribute("postfix");
			Postfix = GetAttValue(raw, "postfix");
			PublicationEndpoint = GetAttValue(raw, "publicationEndpoint");
			PublicationEventEndpoint = GetAttValue(raw, "publicationEventEndpoint");

			if (!string.IsNullOrWhiteSpace(SubscriptionEndpoint) && !string.IsNullOrEmpty(Postfix))
			{
				SetSubscriptionEndpoint($"{SubscriptionEndpoint}.{Postfix}");
			}
			if (!string.IsNullOrWhiteSpace(PublicationEndpoint) && !string.IsNullOrEmpty(Postfix))
			{
				PublicationEndpoint = $"{PublicationEndpoint}.{Postfix}";
			}
			if (!string.IsNullOrWhiteSpace(PublicationEventEndpoint) && !string.IsNullOrEmpty(Postfix))
			{
				PublicationEventEndpoint = $"{PublicationEventEndpoint}.{Postfix}";
			}
			
			RoutingKey = GetAttValue(raw, "routingKey");

			UseExchange = GetAttBoolValue(raw, "useExchange", false);
			DoNotPublish = GetAttBoolValue(raw, "doNotPublish", false);
			
			PrefetchCount = (ushort)GetAttIntValue(raw, "prefetchCount", 5);
			Type = GetAttValue(raw, "type");
			RetryCount = GetAttIntValue(raw, "retryCount");

		}



		
	}
}
