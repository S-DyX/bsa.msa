using System.Collections.Generic;
using Bsa.Msa.Common.Services.MessageHandling;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes
{
	/// <summary>
	/// Creates real handler settings: IMessageHandlerSettings can not be
	/// mocked, its Uid setter is internal and Castle can not implement it.
	/// </summary>
	public static class HandlerSettingsFactory
	{
		/// <summary>
		/// Creates settings with the given handler type and endpoint.
		/// </summary>
		/// <param name="type">Handler type name.</param>
		/// <param name="endpoint">Subscription endpoint.</param>
		/// <returns>The settings.</returns>
		public static MessageHandlerSettings Create(string type, string endpoint)
		{
			var section = ConfigurationFactory.CreateSection(new Dictionary<string, string>
			{
				["type"] = type,
				["subscriptionEndpoint"] = endpoint
			});
			return new MessageHandlerSettings(section);
		}
	}
}
