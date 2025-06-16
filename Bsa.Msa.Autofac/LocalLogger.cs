using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bsa.Msa.Common;

namespace Bsa.Msa.DependencyInjection
{
    /// <summary>
    /// <see cref="ILocalLogger"/>
    /// </summary>
    public sealed class LocalLogger : ILocalLogger
    {
        private readonly ILogger _logger;

        public LocalLogger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("Rabbit");
        }

        public void Warn(string message)
        {
            _logger.LogWarning(message);
        }

        public void Error(string message)
        {
            _logger.LogError(message);
        }

        public void Error(string text, Exception ex)
        {
            _logger.LogError(ex, text);
        }

        public void Info(string message)
        {
            _logger.LogInformation(message);
        }

        public void Debug(string message)
        {
            _logger.LogDebug(message);
        }

        public bool IsErrorEnabled { get; }
        public bool IsDebugEnabled { get; }
        public bool IsFatalEnabled { get; }
        public bool IsInfoEnabled { get; }
        public bool IsWarnEnabled { get; }
    }
}
