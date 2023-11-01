using System;

namespace Bsa.Msa.Common
{
	public sealed class ProcessKeyLockOperation : IDisposable
	{
		private readonly string _id;
		private bool _isClosed;


		public ProcessKeyLockOperation(string id)
		{
			ProcessKeyLock.Instance.Register(id);
			
			_id = id;
		}

		public void Dispose()
		{
			Close();
			
		}

		public void Close()
		{
			//if (!_isClosed)
			{
				ProcessKeyLock.Instance.Release(_id);
				_isClosed = true;
			}
		}
	}
}
