namespace Bsa.Msa.Common.Services.MessageHandling
{
	public interface ISubscribeSettings
	{
		string SubscriptionEndpoint { get; }

		string RoutingKey { get; }

		string Type { get; }

		string Name { get; }
	}
}