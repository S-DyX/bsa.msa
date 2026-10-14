using System.Threading;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Settings;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes
{
	/// <summary>
	/// Command that records its settings, token and disposal.
	/// </summary>
	public sealed class TokenCommand : ICommand
	{
		/// <summary>
		/// The constructor with fewer parameters, must not be chosen.
		/// </summary>
		/// <param name="settings">Command settings.</param>
		public TokenCommand(ISettings settings)
		{
			Settings = settings;
		}

		/// <summary>
		/// The constructor with the most parameters.
		/// </summary>
		/// <param name="settings">Command settings.</param>
		/// <param name="token">Cancelled when the command is stopped.</param>
		public TokenCommand(ISettings settings, CancellationToken token)
		{
			Settings = settings;
			Token = token;
		}

		/// <summary>
		/// Injected settings.
		/// </summary>
		public ISettings Settings { get; }

		/// <summary>
		/// Injected token.
		/// </summary>
		public CancellationToken Token { get; }

		/// <summary>
		/// Number of <see cref="Execute"/> calls.
		/// </summary>
		public int ExecuteCount { get; private set; }

		/// <summary>
		/// Set by <see cref="Dispose"/>.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <inheritdoc/>
		public void Execute()
		{
			ExecuteCount++;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			IsDisposed = true;
		}
	}
}
