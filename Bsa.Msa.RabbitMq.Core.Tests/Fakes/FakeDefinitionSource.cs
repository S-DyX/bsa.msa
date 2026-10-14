using System;
using Bsa.Msa.Common.Services.Sources;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes
{
	/// <summary>
	/// Definition source controlled by the test.
	/// </summary>
	public sealed class FakeDefinitionSource : IServiceDefinitionSource
	{
		/// <summary>
		/// Definitions returned by <see cref="Load"/>.
		/// </summary>
		public ServiceDefinitions Definitions { get; set; } = ServiceDefinitions.Empty;

		/// <inheritdoc/>
		public event EventHandler<ServiceDefinitionsChangedEventArgs> Changed;

		/// <inheritdoc/>
		public ServiceDefinitions Load()
		{
			return Definitions;
		}

		/// <summary>
		/// Replaces the definitions and raises <see cref="Changed"/>.
		/// </summary>
		/// <param name="definitions">New definitions.</param>
		public void Publish(ServiceDefinitions definitions)
		{
			Definitions = definitions;
			Changed?.Invoke(this, new ServiceDefinitionsChangedEventArgs(definitions));
		}
	}
}
