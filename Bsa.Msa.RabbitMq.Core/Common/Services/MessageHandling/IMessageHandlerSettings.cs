
using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.Services.MessageHandling
{
	public interface IMessageHandlerSettings : ISettings
	{
		string SubscriptionEndpoint { get; }

		string RoutingKey { get; }

		string Type { get; }

		bool UseExchange { get; }

		bool Retry { get; }

		int? RetryCount { get; }

		ushort PrefetchCount { get; }

		int DegreeOfParallelism { get; }

		void SetSubscriptionEndpoint(string subscriptionEndpoint);

		int? Ttl { get; }

	}

}
