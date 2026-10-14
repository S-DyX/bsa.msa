using Bsa.Msa.Common;
using Bsa.Msa.RabbitMq.Core.Interfaces;
using Bsa.Msa.RabbitMq.Core.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Bsa.Msa.RabbitMq.Core
{
	/// <inheritdoc />
	public sealed class SimpleConnection : ISimpleConnection
	{
		private readonly IRabbitMqSettings _settings;
		private readonly ILocalLogger _logger;
		private ConnectionFactory _connectionFactory;

		// Guards the connection, the channel and every operation on the channel:
		// IModel is not thread-safe, so all channel calls go through this lock.
		private readonly object _sync = new object();

		private IConnection _connection;

		// Set while AfterConnect handlers run, guarded by _sync.
		private bool _isInAfterConnect;

		private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

		private IConnection Connection
		{
			get
			{
				lock (_sync)
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
					}

					return _connection;
				}
			}
		}

		void _connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
		{
			Close();
		}


		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="connectionString"></param>
		/// <param name="logger"></param>
		public SimpleConnection(string connectionString, ILocalLogger logger)
			: this(new RabbitMqSettings(connectionString), logger)
		{

		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="settings"></param>
		public SimpleConnection(IRabbitMqSettings settings)
			: this(settings, null)
		{
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="logger"></param>
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

		/// <inheritdoc />
		public IModel CreateModel(string name)
		{
			if (ConnectionFactory.AutomaticRecoveryEnabled)
			{
				lock (_sync)
				{
					if (!IsConnected)
						Connect();
					return _model;
				}
			}
			var retryCount = 0;
			int i = 1;
			while (!IsConnected)
			{
				try
				{
					_logger?.Info($"Create model {name}");
					if (!IsConnected)
						Reconnect(name);
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
				}
			}
		}

		/// <inheritdoc />
		public void Close()
		{
			lock (_sync)
			{
				CloseModel();
				CloseConnection();
			}
		}

		private void CloseModel()
		{
			var model = _model;
			_model = null;
			if (model == null) return;

			try
			{
				if (model.IsOpen)
					model.Close();
			}
			catch (Exception ex)
			{
				_logger?.Error(ex.ToString(), ex);
			}

			try
			{
				model.Dispose();
			}
			catch (Exception ex)
			{
				_logger?.Error(ex.ToString(), ex);
			}
		}

		private void CloseConnection()
		{
			var connection = _connection;
			_connection = null;
			if (connection == null) return;

			_logger?.Info("Close connection");
			connection.ConnectionShutdown -= _connection_ConnectionShutdown;
			try
			{
				// An open connection must be closed explicitly,
				// otherwise the socket and the recovery loop stay alive.
				if (connection.IsOpen)
					connection.Close(CloseTimeout);
			}
			catch (Exception ex)
			{
				_logger?.Error(ex.ToString(), ex);
			}

			try
			{
				connection.Dispose();
			}
			catch (Exception ex)
			{
				_logger?.Error(ex.ToString(), ex);
			}
		}

		private readonly List<Action<Func<IModel>>> _actions = new List<Action<Func<IModel>>>();

		private readonly Dictionary<string, Action<Func<IModel>>> _actionsConfigure = new Dictionary<string, Action<Func<IModel>>>();

		/// <inheritdoc />
		public void Add(Action<Func<IModel>> action)
		{
			_actions.Add(action);
		}

		/// <inheritdoc />
		public void Configure(string name, Action<Func<IModel>> action, bool ignoreException = false)
		{
			lock (_actionsConfigure)
			{
				if (_actionsConfigure.ContainsKey(name))
					return;
			}

			while (true)
			{
				try
				{
					lock (_sync)
					{
						action.Invoke(() => CreateModel(name));
					}

					// Cache only a successful configuration, a failed one is retried next time.
					lock (_actionsConfigure)
					{
						_actionsConfigure[name] = action;
					}
					return;
				}
				catch (OperationInterruptedException ex) when (IsRejectedByBroker(ex))
				{
					// The broker rejected the declaration (e.g. other queue arguments):
					// a retry gives the same result, so the error is reported at once.
					_logger?.Error($"Configuration '{name}' was rejected: {ex.Message}", ex);
					if (ignoreException)
						return;
					throw new InvalidOperationException(
						$"Configuration '{name}' was rejected by the broker: {ex.Message}",
						ex);
				}
				catch (Exception ex)
				{
					_logger?.Error(ex.ToString(), ex);
					if (ignoreException)
						return;
					Thread.Sleep(1000);
				}
			}
		}

		/// <inheritdoc />
		public void RemoveConfiguration(string name)
		{
			lock (_actionsConfigure)
			{
				_actionsConfigure.Remove(name);
			}
		}

		/// <inheritdoc />
		public void ReConfigure()
		{
			KeyValuePair<string, Action<Func<IModel>>>[] configs;
			lock (_actionsConfigure)
			{
				configs = _actionsConfigure.ToArray();
			}

			foreach (var config in configs)
			{
				var action = config.Value;

				var isSuscribed = false;
				while (!isSuscribed)
				{
					try
					{
						lock (_sync)
						{
							action.Invoke(() => CreateModel(config.Key));
						}
						isSuscribed = true;
					}
					catch (OperationInterruptedException ex) when (IsRejectedByBroker(ex))
					{
						_logger?.Error($"Configuration '{config.Key}' was rejected: {ex.Message}", ex);
						break;
					}
					catch (Exception ex)
					{
						_logger?.Error(ex.ToString(), ex);
						Thread.Sleep(_sleepTime * 2);
					}
				}
			}
		}

		/// <inheritdoc />
		public void Execute(Action<Func<IModel>> action, string name = null)
		{
			lock (_sync)
			{
				action.Invoke(() => CreateModel(name));
			}
		}

		/// <inheritdoc />
		public event Action BeforeConnect;

		/// <inheritdoc />
		public event Action AfterConnect;

		/// <inheritdoc />
		public void SubscribeAll()
		{
			foreach (var item in _actions)
			{
				var isSubscribed = false;
				while (!isSubscribed)
				{
					try
					{
						lock (_sync)
						{
							item.Invoke(() => CreateModel("unknown"));
						}
						isSubscribed = true;
					}
					catch (Exception ex)
					{
						_logger?.Error(ex.ToString(), ex);
						Thread.Sleep(_sleepTime);
					}
				}
			}
		}

		/// <inheritdoc />
		public void Reconnect(string? name = null)
		{
			if (!IsConnected)
			{
				lock (_sync)
				{
					_logger?.Info($"Reconnect {name}");

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
			lock (_sync)
			{
				if (_model != null && !_model.IsOpen && _connection != null && _connection.IsOpen)
				{
					// The broker closed only the channel (404, 406, ...). Automatic recovery
					// restores connection-level failures only, so the channel is recreated here.
					_logger?.Warn("Channel was closed by the broker, creating a new one");
					CloseModel();
				}

				if (_model != null) return;

				BeforeConnect?.Invoke();
				_model = Connection.CreateModel();

				// A handler that recreates the channel must not run the handlers
				// again: a failing declaration turned into endless recursion.
				if (_isInAfterConnect) return;

				_isInAfterConnect = true;
				try
				{
					AfterConnect?.Invoke();
				}
				finally
				{
					_isInAfterConnect = false;
				}
			}
		}

		private static bool IsRejectedByBroker(OperationInterruptedException ex)
		{
			var code = ex.ShutdownReason?.ReplyCode;
			return code == 403 || code == 404 || code == 406;
		}

		/// <inheritdoc />
		/// <remarks>Only checks the state, the connection is not created here.</remarks>
		public bool IsConnected =>
			_model != null && _model.IsOpen
			&& _connection != null && _connection.IsOpen;

		/// <inheritdoc />
		public void Dispose()
		{
			Close();
		}
	}
}
