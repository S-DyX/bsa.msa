using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.Common.Helpers
{
	public static class InvokeHelper
	{
		public static T SafeReTryInvoke<T>(Func<T> func, Action<Exception> log, int timeout = 500, int tryCount = 10)
		{
			for (var i = 0; i < tryCount; i++)
			{
				try
				{
					if (i > 0)
					{
						Task.Delay(timeout);
					}

					var result = func.Invoke();
					return result;
				}
				catch (Exception ex)
				{
					if (ex.Message == "User was deleted or banned" ||
								ex.Message == "Access denied: this wall available only for community members")
					{
						return default(T);
					}

					log.Invoke(ex);
					timeout += timeout;
				}
			}

			return default(T);
		}
	}
}