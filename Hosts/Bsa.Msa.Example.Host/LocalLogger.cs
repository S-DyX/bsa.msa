using System;
using System.Collections.Generic;
using System.Text;
using Bsa.Msa.Common;

namespace Bsa.Msa.Example.Host
{
	public sealed class LocalLogger : ILocalLogger
	{

		public bool IsErrorEnabled => true;

		public bool IsDebugEnabled => true;

		public bool IsFatalEnabled => true;

		public bool IsInfoEnabled => true;

		public bool IsWarnEnabled => true;

		public void Debug(string message)
		{
			Console.WriteLine(message);
		}

		public void Error(string message)
		{
			Console.WriteLine(message);
		}

		public void Error(string text, Exception ex)
		{
			Console.WriteLine($"{text};{ex}");
		}

		public void Info(string message)
		{
			Console.WriteLine(message);
		}

		public void Warn(string message)
		{
			Console.WriteLine(message);
		}
	}
}
