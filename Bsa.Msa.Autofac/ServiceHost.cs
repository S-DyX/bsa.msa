using Bsa.Msa.Common.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.Autofac
{
	/// <summary>
	/// <see cref="IHostedService"/>
	/// </summary>
	public sealed class ServiceHost : IHostedService, IDisposable
	{
		private readonly IServiceUnitManager _serviceUnitManager;

		public ServiceHost(IServiceUnitManager serviceUnitManager)
		{
			this._serviceUnitManager = serviceUnitManager;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			//serviceProvider.InstallHandlers();
			_serviceUnitManager.Start();
			
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_serviceUnitManager.Stop();
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_serviceUnitManager?.Stop();
		}
	}
}
