using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Settings;
using System;
using System.Threading;

namespace Bsa.Msa.Common.Repeaters
{
	public interface IRepeatingCommandContainerFactory
	{
		IServiceUnit Create(string type, ISettings settings);
	}

	public class RepeatingCommandContainer : IServiceUnit
	{
		private readonly ICommandSettings _settings;
		private readonly IRepeaterFactory _repeaterFactory;
		private readonly ICommandFactory _commandFactory;
		private readonly ILocalLogger _logger;

		private IRepeater _repeater;
		private ICommand _command;
		private Thread _task;

		public RepeatingCommandContainer(
			ISettings settings,
			IRepeaterFactory repeaterFactory,
			ICommandFactory commandFactory,
			ILocalLogger logger = null)
		{
			this._settings = settings as ICommandSettings;
			this._repeaterFactory = repeaterFactory;
			this._commandFactory = commandFactory;
			_logger = logger;
		}
		private bool _isStated = false;

		/// <summary>
		/// Starts the processing unit.
		/// </summary>
		public void Start()
		{
			_repeater = _repeaterFactory.Create(_settings, _logger);
			_repeater.Error += HandleException;
			_logger?.Info($"Start command: {_settings.Name}");

			_isStated = true;
			_repeater.Start
			(

				cancellationToken =>
				{
					var settings = (ISettings)_settings;
					_command = _commandFactory.Create(_settings.Type, settings, cancellationToken);
					_logger?.Info($"Executing command: {_settings.Name}");
					_command.Execute();
					_logger?.Info($"Was executed command: {_settings.Name}");
				}
			);
			_logger?.Info($"Start command end: {_settings.Name}");
		}

		public void Stop()
		{
			_isStated = false;
			if (_repeater != null)
				_repeater.Dispose();

			if (_command != null)
				_command.Dispose();
		}



		public event UnhandledExceptionEventHandler OnError;
		public bool IsStarted => _isStated;
		public string Name => _settings.Name;

		private void HandleException(object sender, UnhandledExceptionEventArgs e)
		{
			if (OnError != null)
				OnError(sender, e);
		}


		public void StartAsync()
		{
			_task = new Thread(Start);
			_task.Start();
		}
	}
}
