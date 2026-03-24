using System;
using Bsa.Msa.Common;
using Bsa.Msa.Common.Services.Interfaces;

namespace Bsa.Msa.DependencyInjection
{
	/// <inheritdoc />
	public sealed class LocalContainer : ILocalContainer
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILocalLogger _localLogger;

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="serviceProvider"></param>
		/// <param name="localLogger"></param>
		public LocalContainer(IServiceProvider serviceProvider, ILocalLogger localLogger)
		{
			this._serviceProvider = serviceProvider;
			_localLogger = localLogger;
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
			try
			{

				var service = _serviceProvider.GetService(type);

				return service;
			}
			catch (ObjectDisposedException ex)
			{
				_localLogger?.Error($"Attempted to resolve {type.FullName} from disposed lifetime scope. " +
				                    $"Check that the scope is properly managed and not disposed too early.", ex);
				throw;
			}

		}
	}
}
