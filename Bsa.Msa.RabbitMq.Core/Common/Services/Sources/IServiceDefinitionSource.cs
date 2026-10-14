using System;

namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// Source of handler and command definitions: services.json by default
	/// (<see cref="ServicesJsonDefinitionSource"/>), or a database and so on.
	/// </summary>
	public interface IServiceDefinitionSource
	{
		/// <summary>
		/// Loads all definitions; called on start and on reload.
		/// </summary>
		/// <returns>Current definitions.</returns>
		ServiceDefinitions Load();

		/// <summary>
		/// Raised when the definitions have changed. The service unit manager
		/// then starts new units, stops removed ones and restarts changed ones.
		/// A source that can not detect changes never raises it.
		/// </summary>
		event EventHandler<ServiceDefinitionsChangedEventArgs> Changed;
	}
}
