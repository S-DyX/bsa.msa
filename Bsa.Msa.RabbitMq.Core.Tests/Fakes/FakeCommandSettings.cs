using System;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Commands.Scheduling;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.RabbitMq.Core.Tests.Fakes
{
	/// <summary>
	/// Command settings with a given schedule.
	/// </summary>
	public sealed class FakeCommandSettings : ICommandSettings
	{
		/// <summary>
		/// Creates the settings.
		/// </summary>
		/// <param name="schedule">Command schedule.</param>
		/// <param name="mode">Concurrency mode.</param>
		public FakeCommandSettings(
			ICommandSchedule schedule,
			RepeaterConcurrentMode mode = RepeaterConcurrentMode.AllowConcurrenceMode)
		{
			Schedule = schedule;
			Mode = mode;
		}

		/// <inheritdoc/>
		public IConfigurationSection Raw => null;

		/// <inheritdoc/>
		public string Name => "TestCommand";

		/// <inheritdoc/>
		public string Type => nameof(TokenCommand);

		/// <inheritdoc/>
		public TimeSpan Period => TimeSpan.Zero;

		/// <inheritdoc/>
		public TimeSpan DueTime => TimeSpan.Zero;

		/// <inheritdoc/>
		public RepeaterConcurrentMode Mode { get; }

		/// <inheritdoc/>
		public DaysOfWeek? DaysOfWeek => null;

		/// <inheritdoc/>
		public ICommandSchedule Schedule { get; }
	}
}
