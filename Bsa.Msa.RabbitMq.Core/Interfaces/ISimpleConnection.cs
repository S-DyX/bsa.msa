using System;
using RabbitMQ.Client;

namespace Bsa.Msa.RabbitMq.Core.Interfaces
{
	public interface ISimpleConnection : IDisposable
	{
		bool IsConnected { get; }

		void Add(Action<Func<IModel>> action);

		event Action BeforeConnect;

		event Action AfterConnect;

		void Configure(string name, Action<Func<IModel>> action, bool ignoreException = false);

		void Execute(Action<Func<IModel>> action);

		void SubscribeAll();

		void Reconnect();
	}

}
