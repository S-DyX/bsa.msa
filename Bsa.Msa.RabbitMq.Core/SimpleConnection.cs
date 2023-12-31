﻿//using System;
//using System.Collections.Generic;
//using System.Threading;
//using RabbitMQ.Client;
//using RabbitMQ.Client.Exceptions;
//using Bsa.Msa.RabbitMq.Core.Common;
//using Bsa.Msa.RabbitMq.Core.Interfaces;
//using Bsa.Msa.RabbitMq.Core.Settings;

//namespace Bsa.Msa.RabbitMq.Core
//{
//	public class SimpleConnection : ISimpleConnection
//	{
//		private readonly IRabbitMqSettings _settings;
//		private readonly ILocalLogger _logger;
//		private ConnectionFactory _connectionFactory;

//		private readonly object _sync = new object();

//		private IConnection _connection;

//		public IConnection Connection
//		{
//			get
//			{
//				if (_connection == null)
//				{
//					try
//					{
//						_connection = ConnectionFactory.CreateConnection();
//						ConnectionFactory.AutomaticRecoveryEnabled = true;
//					}
//					catch (Exception ex)
//					{
//						_logger?.Error($"Host:{ConnectionFactory.HostName},User:{_settings.UserName}", ex);
//						throw new ConnectFailureException($"Host:{ConnectionFactory.HostName},User:{_settings.UserName}", ex);
//					}

//					//_connection.ConnectionShutdown += _connection_ConnectionShutdown;
//				}
//				return _connection;
//			}
//		}

//		void _connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
//		{
//			Close();
//		}



//		public SimpleConnection(string connectionString, ILocalLogger logger)
//			: this(new RabbitMqSettings(connectionString), logger)
//		{

//		}

//		public SimpleConnection(IRabbitMqSettings settings)
//			: this(settings, null)
//		{
//		}
//		public SimpleConnection(IRabbitMqSettings settings, ILocalLogger logger)
//		{
//			_settings = settings;
//			_logger = logger;
//		}

//		private ConnectionFactory ConnectionFactory
//		{
//			get
//			{
//				var hostName = _settings.Host;

//				return _connectionFactory ??= new ConnectionFactory()
//				{
//					HostName = hostName,
//					Password = _settings.Password,
//					UserName = _settings.UserName,
//					Port = _settings.Port,
//					VirtualHost = _settings.VirtualHost ?? "/"
//				};
//			}
//		}

//		private IModel _model;

//		private const int _sleepTime = 500;
//		protected IModel CreateModel()
//		{
//			int i = 1;
//			while (!IsConnected)
//			{
//				try
//				{
//					Reconnect();

//				}
//				catch (Exception ex)
//				{
//					_logger?.Error(ex.ToString(), ex);

//					if (i < 10)
//						i++;
//					var millisecondsTimeout = _sleepTime * i;
//					Thread.Sleep(millisecondsTimeout);
//				}
//			}
//			return _model;

//		}

//		private void Close()
//		{
//			if (_connection != null)
//			{
//				try
//				{
//					_logger?.Warn($"Close Rmq connection");
//					_connection.ConnectionShutdown -= _connection_ConnectionShutdown;
//					using (_connection)
//					{
//					}
//				}
//				catch (ObjectDisposedException ode)
//				{
//					_logger?.Error(ode.ToString(), ode);
//				}
//				catch (Exception ex)
//				{
//					_logger?.Error(ex.ToString(), ex);
//					Thread.Sleep(_sleepTime);
//				}
//				finally
//				{
//					_connection = null;
//				}
//			}
//			if (_model != null)
//			{
//				try
//				{
//					if (!_model.IsClosed)
//						_model.Close();
//				}
//				catch (Exception ex)
//				{
//					_logger?.Error(ex.ToString(), ex);
//					Thread.Sleep(_sleepTime);
//				}
//				finally
//				{
//					_model = null;
//				}
//			}
//		}

//		private readonly List<Action<Func<IModel>>> _actions = new List<Action<Func<IModel>>>();

//		private readonly Dictionary<string, Action<Func<IModel>>> _actionsConfigure = new Dictionary<string, Action<Func<IModel>>>();
//		public void Add(Action<Func<IModel>> action)
//		{
//			_actions.Add(action);
//		}

//		public void Configure(string name, Action<Func<IModel>> action, bool ignoreException = false)
//		{

//			lock (_actionsConfigure)
//			{
//				if (_actionsConfigure.ContainsKey(name))
//					return;
//				if (!_actionsConfigure.ContainsKey(name))
//					_actionsConfigure.Add(name, action);
//			}

//			var isSuscribed = false;
//			while (!isSuscribed)
//			{
//				try
//				{
//					action.Invoke(CreateModel);
//					isSuscribed = true;
//				}
//				catch (Exception ex)
//				{
//					_logger?.Error(ex.ToString(), ex);
//					if (ignoreException)
//						break;
//					Thread.Sleep(1000);
//				}
//			}

//		}


//		public void Execute(Action<Func<IModel>> action)
//		{
//			action.Invoke(CreateModel);
//		}

//		public event Action BeforeConnect;
//		public event Action AfterConnect;

//		public void SubscribeAll()
//		{
//			_logger?.Info($"Rmq Subscribe All");
//			foreach (var item in _actions)
//			{
//				var isSubscribed = false;
//				while (!isSubscribed)
//				{
//					try
//					{
//						item.Invoke(CreateModel);
//						isSubscribed = true;
//					}
//					catch (Exception ex)
//					{
//						_logger?.Error(ex.ToString(), ex);
//						Thread.Sleep(_sleepTime);
//					}
//				}
//			}
//		}

//		public void Reconnect()
//		{
//			if (_model == null)
//			{
//				_logger?.Info($"Rmq reconnect");
//				_model = Connection.CreateModel();
//			}
//		}




//		public bool IsConnected
//		{
//			get
//			{
//				return _model != null && _model.IsOpen && Connection != null && Connection.IsOpen;
//			}
//		}

//		public void Dispose()
//		{
//			Close();
//		}
//	}
//}
