//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Castle.Core.Logging;
//using RabbitMQ.Client;
//using RabbitMQ.Client.Exceptions;
//using XCollectors.Msa.RabbitMq.Core.Settings;

//namespace XCollectors.Msa.RabbitMq.Core
//{
//	public class PoolSimpleConnection : ISimpleConnection
//	{
//		private readonly IRabbitMqSettings _settings;
//		private readonly ILogger _logger;
//		private ConnectionFactory _connectionFactory;

//		private readonly object _sync = new object();

//		private Queue<ISimpleConnection> _connections;

//		public Queue<ISimpleConnection> Connections
//		{
//			get
//			{
//				if (_connections == null)
//				{
//					try
//					{
//						var connections = new Queue<ISimpleConnection>();
//						foreach (var connectionStr in _settings.GetConnections())
//						{
//							connections.Enqueue(new SimpleConnection(connectionStr, _logger));
//							_connections = connections;
//						}

//					}
//					catch (Exception ex)
//					{
//						throw new ConnectFailureException(string.Format("Host:{0},User:{1}", ConnectionFactory.HostName, _settings.Password), ex);
//					}

//					//_connection.ConnectionShutdown += _connection_ConnectionShutdown;
//				}
//				return _connection;
//			}
//		}
//		public PoolSimpleConnection(IRabbitMqSettings settings, ILogger logger)
//		{
//			_settings = settings;
//			_logger = logger;

//		}
//		public bool IsConnected
//		{
//			get
//			{


//			}
//		}

//		public void Add(Action<Func<IModel>> action)
//		{
//			throw new NotImplementedException();
//		}

//		public event Action BeforeConnect;

//		public event Action AfterConnect;

//		public void Configure(string name, Action<Func<IModel>> action)
//		{
//			throw new NotImplementedException();
//		}

//		public void Execute(Action<Func<IModel>> action)
//		{
//			throw new NotImplementedException();
//		}

//		public void SubscribeAll()
//		{
//			throw new NotImplementedException();
//		}

//		public void Reconnect()
//		{
//			throw new NotImplementedException();
//		}
//	}
//}
