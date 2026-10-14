using System;
using System.Threading;

namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// Base class of a source that is polled periodically, e.g. a database.
	/// Implement <see cref="LoadDefinitions"/>; when the loaded definitions
	/// differ from the previous ones, <see cref="Changed"/> is raised.
	/// </summary>
	public abstract class PollingServiceDefinitionSource : IServiceDefinitionSource, IDisposable
	{
		private readonly ILocalLogger _logger;

		private readonly object _sync = new object();

		private readonly Timer _timer;

		private string _fingerprint;

		private int _isPolling;

		/// <summary>
		/// Creates the source and starts polling.
		/// </summary>
		/// <param name="interval">Polling interval.</param>
		/// <param name="logger">Logger for polling errors.</param>
		protected PollingServiceDefinitionSource(TimeSpan interval, ILocalLogger logger = null)
		{
			if (interval <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(
					nameof(interval),
					"Polling interval must be positive.");
			}

			_logger = logger;
			_timer = new Timer(Poll, null, interval, interval);
		}

		/// <inheritdoc/>
		public event EventHandler<ServiceDefinitionsChangedEventArgs> Changed;

		/// <inheritdoc/>
		public ServiceDefinitions Load()
		{
			var definitions = LoadDefinitions() ?? ServiceDefinitions.Empty;
			lock (_sync)
			{
				_fingerprint = definitions.GetFingerprint();
			}

			return definitions;
		}

		/// <summary>
		/// Stops polling.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Loads the definitions from the storage.
		/// Use <see cref="ServiceDefinitionFactory"/> to build them from rows.
		/// </summary>
		/// <returns>Current definitions.</returns>
		protected abstract ServiceDefinitions LoadDefinitions();

		/// <summary>
		/// Stops polling.
		/// </summary>
		/// <param name="disposing">Called from <see cref="Dispose()"/>.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				_timer.Dispose();
		}

		private void Poll(object state)
		{
			// A slow storage must not start overlapping polls.
			if (Interlocked.Exchange(ref _isPolling, 1) == 1) return;

			try
			{
				var definitions = LoadDefinitions() ?? ServiceDefinitions.Empty;
				var fingerprint = definitions.GetFingerprint();
				lock (_sync)
				{
					if (fingerprint == _fingerprint) return;

					_fingerprint = fingerprint;
				}

				Changed?.Invoke(this, new ServiceDefinitionsChangedEventArgs(definitions));
			}
			catch (Exception ex)
			{
				_logger?.Error($"Can not poll service definitions: {ex.Message}", ex);
			}
			finally
			{
				Volatile.Write(ref _isPolling, 0);
			}
		}
	}
}
