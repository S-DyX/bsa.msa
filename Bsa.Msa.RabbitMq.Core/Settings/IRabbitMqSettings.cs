﻿using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.RabbitMq.Core.Settings
{
	public interface IRabbitMqSettings: ISettings
	{
		string Name { get; }
		string UserName { get; }
		string Password { get; }
		string Host { get; }
		string VirtualHost { get; }
		
	}
}
