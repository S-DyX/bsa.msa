using System;
using System.Collections.Generic;
using System.Text;

namespace Bsa.Msa.Common.Services.Interfaces
{
	public interface ILocalContainer
	{
		TType Resolve<TType>();
		object Resolve(Type type);
	}
}
