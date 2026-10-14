using System;

namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// Arguments of <see cref="IServiceDefinitionSource.Changed"/>.
	/// </summary>
	public sealed class ServiceDefinitionsChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Creates the arguments.
		/// </summary>
		/// <param name="definitions">New definitions.</param>
		public ServiceDefinitionsChangedEventArgs(ServiceDefinitions definitions)
		{
			ArgumentNullException.ThrowIfNull(definitions);

			Definitions = definitions;
		}

		/// <summary>
		/// New definitions.
		/// </summary>
		public ServiceDefinitions Definitions { get; }
	}
}
