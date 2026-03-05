using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using FileStorage.Contracts.Rest.Impl.FileStorage;
using Service.Registry.Common;

namespace Bsa.Msa.Example.Host.Handlers
{
	public sealed class ReindexMessage
	{
		public string Body { get; set; }
	}

	public sealed class ReindexMessageHandler : IMessageHandler<ReindexMessage>
	{
		private readonly IBusManager _busManager;

		public ReindexMessageHandler(IBusManager busManager)
		{
			this._busManager = busManager;
		}
		public void Handle(ReindexMessage message)
		{
			message.Body = new string('c', 19698984);
			_busManager.Publish(message);
		}
	}
}
