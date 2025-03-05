using System.Xml.Linq;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.Common.Services.Settings;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.Common.Services.MessageHandling
{
	/// <summary>
	/// Обработчик сообщений
	/// </summary>
	public class MessageHandlerSettings : SettingsConfigurationSection, IMessageHandlerSettings, IServiceUnitSettings
	{
		protected object _lock = new object();
		/// <summary>
		/// Имя обработчика
		/// </summary>
		public string Name { get; protected set; }

		public string RoutingKey { get; protected set; }

		/// <summary>
		/// Тип
		/// </summary>
		public string Type { get; protected set; }

		public bool UseExchange { get; protected set; }

		public MessageHandlerSettings(IConfigurationSection raw)
			: base(raw)
		{
		}
		public MessageHandlerSettings(string name, IConfigurationSection raw)
			: base(raw)
		{
			Name = name;
		}
		public MessageHandlerSettings(string name, string postfix, IConfigurationSection raw)
			: base(raw)
		{
			Name = name;
			Postfix = postfix;
		}
		protected override void LoadSettings(IConfigurationSection raw)
		{
			Name = GetAttValue(raw, "name");
			Type = GetAttValue(raw, "type");
			RoutingKey = GetAttValue(raw, "routingKey");
			Retry = GetAttBoolValue(raw, "retry", false);
			ClearAfterStart=GetAttBoolValue(raw, "clearAfterStart", false);
			AutoDelete = GetAttBoolValue(raw, "autoDelete", false);
			AppendGuid = GetAttBoolValue(raw, "appendGuid", false);
			RetryCount = GetAttIntValue(raw, "retryCount");
			SubscriptionEndpoint = GetAttValue(raw, "subscriptionEndpoint");
			UseExchange = GetAttBoolValue(raw, "useExchange", false);
			PrefetchCount = (ushort)GetAttIntValue(raw, "prefetchCount", 5);
			DegreeOfParallelism = GetAttIntValue(raw, "degreeOfParallelism", 1);
			//Postfix = raw.GetRecursionAttribute("postfix");
			var attValue = GetAttValue(raw, "postfix");
			if (!string.IsNullOrEmpty(attValue))
				Postfix = attValue;
			Ttl = GetAttIntValue(raw, "ttl")?? GetAttIntValue(raw, "Ttl");
            TurnOffInternalQueue = GetAttBoolValue(raw, "turnOffInternalQueue", false);

            if (!string.IsNullOrWhiteSpace(SubscriptionEndpoint) && !string.IsNullOrEmpty(Postfix))
			{
				SetSubscriptionEndpoint($"{SubscriptionEndpoint}.{Postfix}");
			}

		}

		public string SubscriptionEndpoint
		{
			get; protected set;
		}


		public int DegreeOfParallelism
		{
			get;
			protected set;
		}


		public bool Retry
		{
			get;
			protected set;
		}

		public int? RetryCount { get; protected set; }


		public ushort PrefetchCount
		{
			get;
			protected set;
		}

		protected bool _isChanged;
		public void SetSubscriptionEndpoint(string subscriptionEndpoint)
		{
			if (!_isChanged)
			{
				lock (_lock)
				{
					if (!_isChanged)
					{
						_isChanged = true;
						SubscriptionEndpoint = subscriptionEndpoint;
					}
				}
			}
		}

		public int? Ttl
		{
			get;
			protected set;
		}

		public bool ClearAfterStart
		{
			get;
			protected set;
		}
		public bool AutoDelete
		{
			get;
			protected set;
		}
		public bool AppendGuid
		{
			get;
			protected set;
		}

        public bool TurnOffInternalQueue
        {
            get;
            protected set;
        }

        public string Postfix
		{
			get;
			set;
		}
	}
}
