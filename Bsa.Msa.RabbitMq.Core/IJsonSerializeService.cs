using System;
using System.Collections.Generic;
using System.Text;

namespace Bsa.Msa.RabbitMq.Core
{
	public interface ISerializeService
	{
		TValue Deserialize<TValue>(string value);

		string Serialize(object obj);
	}
}
