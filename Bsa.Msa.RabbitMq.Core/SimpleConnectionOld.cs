using Bsa.Msa.Common;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bsa.Msa.RabbitMq.Core
{
	public class SimpleConnection : ISimpleConnection
	{
		private readonly IRabbitMqSettings _settings;
		private readonly ILocalLogger _logger;
		private ConnectionFactory _connectionFactory;

		private static object _sync = new object();

		private IConnection _connection;

		public IConnection Connection
		{
			get
			{
				if (_connection == null)
				{
					try
					{ 
						_connection = ConnectionFactory.CreateConnection();
					}
					catch (Exception ex)
					{
						_logger?.Error($"Host:{ConnectionFactory.HostName},User:{_settings.UserName}", ex);
						throw new ConnectFailureException($"Host:{ConnectionFactory.HostName},User:{_settings.UserName}", ex);
					}

					//_connection.ConnectionShutdown += _connection_ConnectionShutdown;
				}
				return _connection;
			}
		}

		void _connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
		{
			Close();
		}



		public SimpleConnection(string connectionString, ILocalLogger logger)
			: this(new RabbitMqSettings(connectionString), logger)
		{

		}

		public SimpleConnection(IRabbitMqSettings settings)
			: this(settings, null)
		{
		}
		public SimpleConnection(IRabbitMqSettings settings, ILocalLogger logger)
		{
			_settings = settings;
			_logger = logger;
		}

		private ConnectionFactory ConnectionFactory
		{
			get
			{
				var hostName = _settings.Host;

				var connectionFactory = _connectionFactory ??= new ConnectionFactory()
				{
					HostName = hostName,
					Password = _settings.Password,
					UserName = _settings.UserName,
					AutomaticRecoveryEnabled = true,
					VirtualHost = _settings.VirtualHost ?? "/"
				};
				return connectionFactory;
			}
		}

		private IModel _model;

		private const int _sleepTime = 500;
		public IModel CreateModel(string name)
		{
			if (ConnectionFactory.AutomaticRecoveryEnabled)
			{
				Connect();
				return _model;
			}
			var retryCount = 0;
			int i = 1;
			while (!IsConnected)
			{
				try
				{
					_logger?.Info($"Create model {name}");
					if (!IsConnected)
						Reconnect();
					retryCount++;
					if (retryCount > 10)
					{ 
						Thread.Sleep(2000);
						_logger?.Info($"CreateModel Sleep model {name}");
					}

				}
				catch (Exception ex)
				{
					_logger?.Error($"name:{name}; message:{ex.ToString()}", ex);

					if (i < 10)
						i++;
					var millisecondsTimeout = _sleepTime * i;
					Thread.Sleep(millisecondsTimeout);
				}
			}
			return _model;

		}
		private void CloseL()
		{
			if (_connection != null)
			{
				_logger?.Info("Close connection");
				try
				{
					if (!_connection.IsOpen)
					{
						var temp = _connection;
						_connection = null;
						temp.ConnectionShutdown -= _connection_ConnectionShutdown;
						using (temp)
						{
						}
					}
				}
				catch (ObjectDisposedException ode)
				{
					_logger?.Error(ode.ToString(), ode);
				}
				catch (Exception ex)
				{
					_logger?.Error(ex.ToString(), ex);
					Task.Delay(_sleepTime);
				}
			}
			if (_model != null)
			{
				try
				{
					var temp = _model;
					
					if (!temp.IsOpen)
					{
						_model = null;
					}
				}
				catch (Exception ex)
				{
					_logger?.Error(ex.ToString(), ex);
					Task.Delay(_sleepTime);
				}
				finally
				{
				}
			}
		}
		private void Close()
		{
			if (_connection != null)
			{
				try
				{
					if (!_connection.IsOpen)
					{
						var temp = _connection;
						_connection = null;
						temp.ConnectionShutdown -= _connection_ConnectionShutdown;
						using (temp)
						{
						}
					}
				}
				catch (ObjectDisposedException ode)
				{
					_logger?.Error(ode.ToString(), ode);
				}
				catch (Exception ex)
				{
					_logger?.Error(ex.ToString(), ex);
					Task.Delay(_sleepTime);
				}
				finally
				{
					_connection = null;

				}
			}
			if (_model != null)
			{
				try
				{
					if (!_model.IsClosed)
						_model.Close();
				}
				catch (Exception ex)
				{
					_logger?.Error(ex.ToString(), ex);
					Task.Delay(_sleepTime);
				}
				finally
				{
					_model = null;
				}
			}
		}

		private readonly List<Action<Func<IModel>>> _actions = new List<Action<Func<IModel>>>();

		private readonly Dictionary<string, Action<Func<IModel>>> _actionsConfigure = new Dictionary<string, Action<Func<IModel>>>();
		public void Add(Action<Func<IModel>> action)
		{
			_actions.Add(action);
		}

		public void Configure(string name, Action<Func<IModel>> action, bool ignoreException = false)
		{

			lock (_actionsConfigure)
			{
				if (_actionsConfigure.ContainsKey(name))
					return;
				if (!_actionsConfigure.ContainsKey(name))
					_actionsConfigure.Add(name, action);
			}

			var isSuscribed = false;
			while (!isSuscribed)
			{
				try
				{
					action.Invoke(() => CreateModel(name));
					isSuscribed = true;
				}
				catch (Exception ex)
				{
					_logger?.Error(ex.ToString(), ex);
					if (ignoreException)
						break;
					Task.Delay(1000);
				}
			}

		}

		public void ReConfigure()
		{
			foreach (var config in _actionsConfigure)
			{
				var action = config.Value;

				var isSuscribed = false;
				while (!isSuscribed)
				{
					try
					{
						action.Invoke(() => CreateModel(config.Key));
						isSuscribed = true;
					}
					catch (Exception ex)
					{
						_logger?.Error(ex.ToString(), ex);
						Task.Delay(_sleepTime * 2);
						// добавить event
					}
				}
			}

		}

		private readonly object _syncSend = new object();
		public void Execute(Action<Func<IModel>> action, string name = null)
		{
			lock (_syncSend)
			{
				action.Invoke(() => CreateModel(name));
			}

		}

		public event Action BeforeConnect;
		public event Action AfterConnect;

		public void SubscribeAll()
		{
			foreach (var item in _actions)
			{
				var isSubscribed = false;
				while (!isSubscribed)
				{
					try
					{
						item.Invoke(() => CreateModel("unknown"));
						isSubscribed = true;
					}
					catch (Exception ex)
					{
						_logger?.Error(ex.ToString(), ex);
						Task.Delay(_sleepTime);
						// добавить event
					}
				}
			}
		}

		public void Reconnect()
		{
			if (!IsConnected)
			{
				lock (_sync)
				{
					_logger?.Info("Reconnect");

					if (ConnectionFactory.AutomaticRecoveryEnabled)
					{
						Connect();
					}
					else if (!IsConnected)
					{
						CloseL();
						Connect();
					}
				}
			}

		}

		private void Connect()
		{
			BeforeConnect?.Invoke();
			if (_model == null)
			{
				_model = Connection.CreateModel();
				AfterConnect?.Invoke();
			}
		}


		public bool IsConnected => _model != null && _model.IsOpen && Connection != null && Connection.IsOpen;

		public void Dispose()
		{
			Close();
		}
	}
}
