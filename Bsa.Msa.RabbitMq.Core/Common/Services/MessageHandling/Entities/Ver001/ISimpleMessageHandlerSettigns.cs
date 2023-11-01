using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.Common.Services.MessageHandling.Entities.Ver001
{
	public interface ISimpleMessageHandlerSettigns : IMessageHandlerSettings
	{
		string PublicationEndpoint { get; }

		string PublicationEventEndpoint { get; }

		/// <summary>
		/// Нужно ли пересылать сообщение дальше
		/// </summary>
		bool DoNotPublish { get; }
	}

	public static class MessageHandlerSettingsHelper
	{
		public static ISimpleMessageHandlerSettigns As(this ISettings settings)
		{
			var simpleMessageHandlerSettigns = new SimpleMessageHandlerSettigns(settings.Raw);
			simpleMessageHandlerSettigns.SetSubscriptionEndpoint(simpleMessageHandlerSettigns.SubscriptionEndpoint);
			return simpleMessageHandlerSettigns;
		}

		public static string GetAttValue(ISettings settings, string name)
		{
			var attribute = settings.Raw.GetSection(name);
			if (attribute != null)
				return attribute.Value;
			return string.Empty;
		}

		public static string GetAttStrValue(this ISettings settings, string name, string defaultValue)
		{
			var attribute = settings.Raw.GetSection(name);
			if (attribute?.Value != null)
				return attribute.Value;
			return defaultValue;
		}
		public static int GetAttIntValue(this ISettings settings, string name, int defaultValue)
		{
			var value = GetAttValue(settings, name);
			if (string.IsNullOrEmpty(value))
				return defaultValue;
			return int.Parse(value);
		}

		public static double GetAttDoubleValue(this ISettings settings, string name, double defaultValue)
		{
			var value = GetAttValue(settings, name);
			if (string.IsNullOrEmpty(value))
				return defaultValue;
			return double.Parse(value);
		}
		public static bool GetAttBoolValue(this ISettings settings, string name, bool defaultValue)
		{
			var value = GetAttValue(settings, name);
			if (string.IsNullOrEmpty(value))
				return defaultValue;
			return bool.Parse(value);
		}
	}
}
