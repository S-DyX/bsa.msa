using System;

namespace Bsa.Msa.Common
{
	public interface ILocalLogger
	{
		bool IsErrorEnabled { get; }

		bool IsDebugEnabled { get; }

		bool IsFatalEnabled { get; }
		bool IsInfoEnabled { get; }
		bool IsWarnEnabled { get; }
		void Warn(string message);
		void Error(string message);

		void Error(string text, Exception ex);

		void Info(string message);

		void Debug(string message);
	}
}
