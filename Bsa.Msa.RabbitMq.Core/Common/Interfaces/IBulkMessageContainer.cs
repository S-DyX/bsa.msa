using System;
using System.Collections.Generic;
using System.Text;

namespace Bsa.Msa.Common.Interfaces
{
	public interface IBulkMessageContainer<TValue>
	{
		void Add(List<TValue> messages, Func<TValue, string> getKey);
		List<TValue> Get();

		List<TValue> Get(int size);

		List<TValue> Get(int size, int seconds);

		int Count();
	}
}
