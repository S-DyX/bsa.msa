using System;
using System.Threading.Tasks;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Settings;

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

		private IRepeater _repeater;
		private ICommand _command;
		private Task _task;

		public RepeatingCommandContainer(
			ISettings settings,
			IRepeaterFactory repeaterFactory,
			ICommandFactory commandFactory)
		{
			this._settings = settings as ICommandSettings;
			this._repeaterFactory = repeaterFactory;
			this._commandFactory = commandFactory;
		}

		/// <summary>
		/// Starts the processing unit.
		/// </summary>
		public void Start()
		{
			_repeater = _repeaterFactory.Create(_settings.DueTime, _settings.Period, _settings.Mode);
			_repeater.Error += HandleException;



			_repeater.Start
			(
				cancellationToken =>
				{
					var settings = (ISettings)_settings;
					_command = _commandFactory.Create(_settings.Type, settings, cancellationToken);
					_command.Execute();
				}
			);
		}

		public void Stop()
		{
			if (_repeater != null)
				_repeater.Dispose();

			if (_command != null)
				_command.Dispose();
		}



		public event UnhandledExceptionEventHandler OnError;

		private void HandleException(object sender, UnhandledExceptionEventArgs e)
		{
			if (OnError != null)
				OnError(sender, e);
		}


		public void StartAsync()
		{
			_task = new Task(Start);
			_task.Start();
		}
	}
}
