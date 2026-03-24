using System;

namespace Bsa.Msa.Common
{
	/// <summary>
	/// Provides an abstraction layer for logging functionality that can work with any underlying logging framework.
	/// This interface defines a common contract for logging operations, allowing seamless integration with
	/// various logging implementations such as Serilog, NLog, log4net, Microsoft.Extensions.Logging, etc.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The ILocalLogger interface is designed to be a minimal, dependency-free abstraction that can be
	/// implemented by any logging framework. It provides essential logging methods and level checks
	/// to enable conditional logging based on the current logging configuration.
	/// </para>
	/// <para>
	/// This abstraction is particularly useful when:
	/// - Building libraries that need to log but shouldn't force a specific logging framework
	/// - Creating testable components where logging can be mocked or verified
	/// - Migrating between different logging frameworks without changing consumer code
	/// </para>
	/// </remarks>
	public interface ILocalLogger
	{
		/// <summary>
		/// Gets a value indicating whether error-level logging is enabled.
		/// This can be used to avoid expensive operations when error logging is disabled.
		/// </summary>
		/// <returns>true if error logging is enabled and configured for the current context; otherwise, false.</returns>
		/// <example>
		/// <code>
		/// if (logger.IsErrorEnabled)
		/// {
		///     logger.Error($"Complex operation failed with data: {expensiveObject.ToString()}");
		/// }
		/// </code>
		/// </example>
		bool IsErrorEnabled { get; }

		/// <summary>
		/// Gets a value indicating whether debug-level logging is enabled.
		/// Debug logs are typically used during development and troubleshooting.
		/// </summary>
		/// <returns>true if debug logging is enabled; otherwise, false.</returns>
		/// <remarks>
		/// Debug logs should contain detailed diagnostic information that may include sensitive data.
		/// Always check IsDebugEnabled before performing expensive operations for debug logs.
		/// </remarks>
		bool IsDebugEnabled { get; }

		/// <summary>
		/// Gets a value indicating whether fatal-level logging is enabled.
		/// Fatal logs represent critical application failures that require immediate attention.
		/// </summary>
		/// <returns>true if fatal logging is enabled; otherwise, false.</returns>
		/// <remarks>
		/// Fatal errors typically indicate that the application is about to crash or become unusable.
		/// These logs should be monitored closely in production environments.
		/// </remarks>
		bool IsFatalEnabled { get; }

		/// <summary>
		/// Gets a value indicating whether informational-level logging is enabled.
		/// Info logs provide general application flow and business operation information.
		/// </summary>
		/// <returns>true if informational logging is enabled; otherwise, false.</returns>
		/// <remarks>
		/// Info logs should track important business events, user actions, and system state changes.
		/// They are typically enabled in production environments to monitor application behavior.
		/// </remarks>
		bool IsInfoEnabled { get; }

		/// <summary>
		/// Gets a value indicating whether warning-level logging is enabled.
		/// Warning logs indicate potentially harmful situations that don't necessarily cause failures.
		/// </summary>
		/// <returns>true if warning logging is enabled; otherwise, false.</returns>
		/// <remarks>
		/// Warnings are used for situations that are not errors but deserve attention,
		/// such as deprecated API usage, retryable operations, or unexpected data states.
		/// </remarks>
		bool IsWarnEnabled { get; }

		/// <summary>
		/// Logs a warning message.
		/// Use this method to log potentially harmful situations that don't necessarily cause application failures.
		/// </summary>
		/// <param name="message">The warning message to log.</param>
		/// <exception cref="ArgumentNullException">Thrown when message is null or empty, depending on implementation.</exception>
		/// <example>
		/// <code>
		/// logger.Warn("Database connection attempt 3 failed, retrying...");
		/// </code>
		/// </example>
		void Warn(string message);

		/// <summary>
		/// Logs an error message.
		/// Use this method to log application errors that don't necessarily cause application termination.
		/// </summary>
		/// <param name="message">The error message to log.</param>
		/// <exception cref="ArgumentNullException">Thrown when message is null or empty, depending on implementation.</exception>
		/// <example>
		/// <code>
		/// logger.Error("Failed to process payment transaction");
		/// </code>
		/// </example>
		void Error(string message);

		/// <summary>
		/// Logs an error message with an associated exception.
		/// Use this method to log errors with detailed exception information including stack traces.
		/// </summary>
		/// <param name="text">The error message describing the error context.</param>
		/// <param name="ex">The exception that was caught and should be logged.</param>
		/// <exception cref="ArgumentNullException">Thrown when text or ex is null, depending on implementation.</exception>
		/// <example>
		/// <code>
		/// try
		/// {
		///     // Some operation
		/// }
		/// catch (Exception ex)
		/// {
		///     logger.Error("Failed to process request", ex);
		/// }
		/// </code>
		/// </example>
		void Error(string text, Exception ex);

		/// <summary>
		/// Logs an informational message.
		/// Use this method to track normal application flow, important business events, and system state changes.
		/// </summary>
		/// <param name="message">The informational message to log.</param>
		/// <exception cref="ArgumentNullException">Thrown when message is null or empty, depending on implementation.</exception>
		/// <example>
		/// <code>
		/// logger.Info($"User {userId} successfully logged in at {DateTime.UtcNow}");
		/// logger.Info("Application started successfully");
		/// </code>
		/// </example>
		void Info(string message);

		/// <summary>
		/// Logs a debug message.
		/// Use this method for detailed diagnostic information during development and troubleshooting.
		/// </summary>
		/// <param name="message">The debug message to log.</param>
		/// <remarks>
		/// <para>
		/// Debug logs should only be enabled in development or when troubleshooting production issues.
		/// They may contain sensitive information or be performance-intensive.
		/// </para>
		/// <para>
		/// Always check IsDebugEnabled before constructing expensive debug messages:
		/// <code>
		/// if (logger.IsDebugEnabled)
		/// {
		///     logger.Debug($"User state: {JsonSerializer.Serialize(userData)}");
		/// }
		/// </code>
		/// </para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">Thrown when message is null or empty, depending on implementation.</exception>
		/// <example>
		/// <code>
		/// logger.Debug("Entering ProcessOrder method");
		/// logger.Debug($"Processing order {orderId} with {itemCount} items");
		/// </code>
		/// </example>
		void Debug(string message);
	}
