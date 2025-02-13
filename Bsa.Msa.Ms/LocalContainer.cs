using System;
using Bsa.Msa.Common.Services.Interfaces;

namespace Bsa.Msa.DependencyInjection
{
	public sealed class LocalContainer : ILocalContainer
	{
		private readonly IServiceProvider _serviceProvider;

		public LocalContainer(IServiceProvider serviceProvider)
		{
			this._serviceProvider = serviceProvider;
		}
		public TType Resolve<TType>()
		{
			return (TType)_serviceProvider.GetService(typeof(TType));
		}

		public object Resolve(Type type)
		{

			var service = _serviceProvider.GetService(type);

			return service;
		}
	}
}
