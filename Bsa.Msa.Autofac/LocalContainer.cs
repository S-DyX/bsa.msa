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
						foreach (var s in r.Services)
						{


							// Check if provider is still valid
							if (_provider?.LifetimeScope == null)
							{
								throw new InvalidOperationException(
									$"Cannot resolve {type.FullName}: LifetimeScope is null or has been disposed");
							}

							if (s.Description.Equals(type.FullName))
							{
								var result = ResolveService(s);
								if (result != null)
									return result;
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

		private object ResolveService(Service s)
		{
			try
			{
				var ts = s as TypedService;
				if (ts != null)
				{
					return _serviceProvider.GetService(ts.ServiceType);
				}

			}
			catch (Exception e)
			{
				_localLogger?.Error($"Attempted to resolve {s.Description} from disposed lifetime scope. " +
				                    $"Check that the scope is properly managed and not disposed too early.", e);
			}
			try
			{
				var ks = s as KeyedService;
				if (ks != null)
				{
					return _serviceProvider.GetService(ks.ServiceType);
				}

			}
			catch (Exception e)
			{
				_localLogger?.Error($"Attempted to resolve {s.Description} from disposed lifetime scope. " +
				                    $"Check that the scope is properly managed and not disposed too early.", e);
			}
		

			return null;
		}
	}
}
