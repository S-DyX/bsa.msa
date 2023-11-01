using System;

namespace Bsa.Msa.Common.Services.MessageHandling
{
	public interface IReceiveSettings
	{
		string SubscriptionEndpoint { get; }

		IMessageHandlerSettings[] MessageHandlers { get; }

	}
}