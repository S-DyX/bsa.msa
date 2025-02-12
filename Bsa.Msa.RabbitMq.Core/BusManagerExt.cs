using System;
using System.Collections.Generic;
using System.Text;
using Bsa.Msa.Common;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;

namespace Bsa.Msa.RabbitMq.Core
{
	public static class BusManagerExt
	{
		public static IBusManager CreateBus(this IRabbitMqSettings settings, ILocalLogger logger = null, ISerializeService serializeService = null, ISimpleBusNaming busNaming = null)
		{
			var connection = new SimpleConnection(settings);	
			return new BusManager(new SimpleBus(connection, logger, serializeService, busNaming));
		}

		public static IBusManager CreateBus(this string connection, ILocalLogger logger = null, ISerializeService serializeService = null, ISimpleBusNaming busNaming = null)
		{
			return CreateBus(new RabbitMqSettings(connection), logger, serializeService, busNaming);
		}
	}

}
