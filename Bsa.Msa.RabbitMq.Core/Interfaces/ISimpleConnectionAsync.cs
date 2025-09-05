//using System;
//using RabbitMQ.Client;

//namespace Bsa.Msa.RabbitMq.Core.Interfaces
//{
//	public interface ISimpleConnection : IDisposable
//	{
//		bool IsConnected { get; }

//		void Add(Action<Func<IChannel>> action);

//		event Action BeforeConnect;

//		event Action AfterConnect;

//		void Configure(string name, Action<Func<IChannel>> action, bool ignoreException = false);

//		void Execute(Action<Func<IChannel>> action, string name = null);

//		void SubscribeAll();

//		void Reconnect();
//	}

//}
