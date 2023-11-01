using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Bsa.Msa.RabbitMq.Core
{
	public sealed class SerializeService : ISerializeService
	{
		public TValue Deserialize<TValue>(string value)
		{
			return JsonSerializer.Deserialize<TValue>(value);
		}

		public string Serialize(object obj)
		{
			return JsonSerializer.Serialize(obj);
		}
	}
}
