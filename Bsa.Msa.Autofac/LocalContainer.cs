using Bsa.Msa.Common.Services.Interfaces;
using System;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bsa.Msa.Autofac
{
	public sealed class LocalContainer : ILocalContainer
	{
		private readonly IServiceProvider serviceProvider;
		private readonly AutofacServiceProvider _provider;

		public LocalContainer(IServiceProvider serviceProvider)
		{
			this.serviceProvider = serviceProvider;
			_provider = serviceProvider as AutofacServiceProvider;
		}
		public TType Resolve<TType>()
		{
			return (TType)serviceProvider.GetService(typeof(TType));
		}

		public object Resolve(Type type)
		{

			var service = serviceProvider.GetService(type);
			if (service == null && _provider != null)
			{
				foreach (var r in _provider.LifetimeScope.ComponentRegistry.Registrations)
				{
					foreach (TypedService s in r.Services)
					{
						if (s == null)
							continue;

						if (s.Description.Equals(type.FullName))
						{
							var temp = serviceProvider.GetService(s.ServiceType);
							return temp;
						}
					}


				}


			}

			return service;
		}
	}
}
