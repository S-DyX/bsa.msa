using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Bsa.Msa.Common;
using Bsa.Msa.Common.Services.Interfaces;
using System;

namespace Bsa.Msa.Autofac
{
	/// <inheritdoc />
	public sealed class LocalContainer : ILocalContainer
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILocalLogger _localLogger;
		private readonly AutofacServiceProvider _provider;

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="serviceProvider"></param>
		/// <param name="localLogger"></param>
		public LocalContainer(IServiceProvider serviceProvider, ILocalLogger localLogger = null)
		{
			this._serviceProvider = serviceProvider;
			_localLogger = localLogger;
			_provider = serviceProvider as AutofacServiceProvider;
		}

		/// <inheritdoc />
		public TType Resolve<TType>()
		{
			var type = typeof(TType);
			try
			{
				return (TType)_serviceProvider.GetService(type);
			}

			catch (ObjectDisposedException ex)
			{
				_localLogger?.Error($"Attempted to resolve {type.FullName} from disposed lifetime scope. " +
									$"Check that the scope is properly managed and not disposed too early.", ex);
				throw;
			}
		}

		/// <inheritdoc />
		public object Resolve(Type type)
		{

			var service = _serviceProvider.GetService(type);
			if (service == null && _provider != null)
			{
				try
				{
					foreach (var r in _provider.LifetimeScope.ComponentRegistry.Registrations)
					{
						foreach (TypedService s in r.Services)
						{
							if (s == null)
								continue;

							// Check if provider is still valid
							if (_provider?.LifetimeScope == null)
							{
								throw new InvalidOperationException(
									$"Cannot resolve {type.FullName}: LifetimeScope is null or has been disposed");
							}

							if (s.Description.Equals(type.FullName))
							{
								var temp = _serviceProvider.GetService(s.ServiceType);
								return temp;
							}
						}


					}
				}
				catch (ObjectDisposedException ex)
				{
					_localLogger?.Error($"Attempted to resolve {type.FullName} from disposed lifetime scope. " +
										$"Check that the scope is properly managed and not disposed too early.", ex);
					throw;
				}
			}

			return service;
		}
	}
}
