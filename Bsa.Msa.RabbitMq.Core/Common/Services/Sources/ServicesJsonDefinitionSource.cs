using System;
using Bsa.Msa.Common.Services.Settings;

namespace Bsa.Msa.Common.Services.Sources
{
	/// <summary>
	/// Default source: the "services" section of services.json
	/// (<see cref="IServicesSettings"/>).
	/// </summary>
	public sealed class ServicesJsonDefinitionSource : IServiceDefinitionSource
	{
		private readonly IServicesSettings _servicesSettings;

		/// <summary>
		/// Reads services.json from the application folder.
		/// </summary>
		public ServicesJsonDefinitionSource()
			: this(new ServicesSettings())
		{
		}

		/// <summary>
		/// Uses already loaded services settings.
		/// </summary>
		/// <param name="servicesSettings">Services settings.</param>
		public ServicesJsonDefinitionSource(IServicesSettings servicesSettings)
		{
			ArgumentNullException.ThrowIfNull(servicesSettings);

			_servicesSettings = servicesSettings;
		}

		/// <inheritdoc/>
		/// <remarks>The file is read once, so the event is never raised.</remarks>
		public event EventHandler<ServiceDefinitionsChangedEventArgs> Changed
		{
			add { }
			remove { }
		}

		/// <inheritdoc/>
		public ServiceDefinitions Load()
		{
			return ServiceDefinitions.FromServices(_servicesSettings.GetServices());
		}
	}
}
