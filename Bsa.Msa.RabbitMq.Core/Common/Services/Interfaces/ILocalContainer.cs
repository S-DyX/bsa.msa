using System;
using System.Collections.Generic;
using System.Text;

namespace Bsa.Msa.Common.Services.Interfaces
{
	/// <summary>
	/// Provides a local service resolution container that wraps the underlying dependency injection container.
	/// This interface serves as an abstraction for resolving services from the current scope/lifetime context.
	/// </summary>
	public interface ILocalContainer
	{
		/// <summary>
		/// Resolves a service of the specified type TType from the local container.
		/// </summary>
		/// <typeparam name="TType">The type of service to resolve.</typeparam>
		/// <returns>The resolved service instance of type TType.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the service cannot be resolved or the container is disposed.</exception>
		/// <exception cref="ObjectDisposedException">Thrown when the underlying lifetime scope has been disposed.</exception>
		/// <example>
		/// <code>
		/// var service = localContainer.Resolve&lt;IMyService&gt;();
		/// </code>
		/// </example>
		TType Resolve<TType>();

		/// <summary>
		/// Resolves a service of the specified Type from the local container.
		/// This overload allows for dynamic type resolution when the type is not known at compile time.
		/// </summary>
		/// <param name="type">The Type of service to resolve.</param>
		/// <returns>The resolved service instance, or null if the service cannot be resolved.</returns>
		/// <exception cref="ObjectDisposedException">Thrown when the underlying lifetime scope has been disposed.</exception>
		/// <exception cref="ArgumentNullException">Thrown when the type parameter is null.</exception>
		/// <example>
		/// <code>
		/// Type myType = typeof(IMyService);
		/// var service = localContainer.Resolve(myType);
		/// </code>
		/// </example>
		object Resolve(Type type);
	}
}
