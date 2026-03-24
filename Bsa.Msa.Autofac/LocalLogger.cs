using Bsa.Msa.Common;
using Microsoft.Extensions.Logging;
using System;

namespace Bsa.Msa.DependencyInjection
{
	/// <inheritdoc />
	public sealed class LocalLogger : ILocalLogger
	{
		private readonly ILogger _logger;

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="loggerFactory"></param>
		public LocalLogger(ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger("Rabbit");
		}

		/// <inheritdoc />
		public void Warn(string message)
		{
			_logger.LogWarning(message);
		}

		/// <inheritdoc />
		public void Error(string message)
		{
			_logger.LogError(message);
		}

		/// <inheritdoc />
		public void Error(string text, Exception ex)
		{
			_logger.LogError(ex, text);
		}

		/// <inheritdoc />
		public void Info(string message)
		{
			_logger.LogInformation(message);
		}

		/// <inheritdoc />
		public void Debug(string message)
		{
			_logger.LogDebug(message);
		}

		/// <inheritdoc />
		public bool IsErrorEnabled { get; }

		/// <inheritdoc />
		public bool IsDebugEnabled { get; }

		/// <inheritdoc />
		public bool IsFatalEnabled { get; }

		/// <inheritdoc />
		public bool IsInfoEnabled { get; }

		/// <inheritdoc />
		public bool IsWarnEnabled { get; }
	}
}
