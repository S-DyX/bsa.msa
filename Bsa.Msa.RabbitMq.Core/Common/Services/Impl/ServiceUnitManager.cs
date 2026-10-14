using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bsa.Msa.Common.Repeaters;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.Interfaces;
using Bsa.Msa.Common.Services.MessageHandling;
using Bsa.Msa.Common.Services.Settings;
using Bsa.Msa.Common.Services.Sources;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.RabbitMq.Core;
using Bsa.Msa.RabbitMq.Core.Common;
using Bsa.Msa.RabbitMq.Core.Interfaces;

namespace Bsa.Msa.Common.Services.Impl
{
	/// <summary>
	/// Starts handlers and commands loaded from <see cref="IServiceDefinitionSource"/>
	/// (services.json by default) and controls them at runtime
	/// (<see cref="IServiceUnitController"/>). Units are identified by name.
	/// </summary>
	public sealed class ServiceUnitManager : IServiceUnitManager, IServiceUnitController
	{
		private readonly IServiceDefinitionSource _source;

		private readonly ICommandFactory _commandFactory;

		private readonly IRepeaterFactory _repeaterFactory;

		private readonly ISubscriberFactory _subscriberFactory;

		private readonly IMessageHandlerFactory _messageHandlerFactory;

		private readonly ILocalLogger _logger;

		private readonly InternalBus _internalBus;

		private readonly object _sync = new object();

		private readonly Dictionary<string, UnitEntry> _handlers =
			new Dictionary<string, UnitEntry>(StringComparer.Ordinal);

		private readonly Dictionary<string, UnitEntry> _commands =
			new Dictionary<string, UnitEntry>(StringComparer.Ordinal);

		private bool _isStarted;

		/// <summary>
		/// Creates the manager that reads services.json.
		/// </summary>
		/// <param name="servicesSection">Services from services.json.</param>
		/// <param name="simpleBus">Not used, kept for compatibility.</param>
		/// <param name="commandFactory">Command factory.</param>
		/// <param name="repeaterFactory">Repeater factory.</param>
		/// <param name="subscriberFactory">Subscriber factory.</param>
		/// <param name="messageHandlerFactory">Handler factory.</param>
		public ServiceUnitManager(IServicesSettings servicesSection,
			ISimpleBus simpleBus,
			ICommandFactory commandFactory,
			IRepeaterFactory repeaterFactory,
			ISubscriberFactory subscriberFactory,
			IMessageHandlerFactory messageHandlerFactory)
		: this(servicesSection, simpleBus, commandFactory, repeaterFactory, subscriberFactory, messageHandlerFactory, null, null)
		{
		}

		/// <summary>
		/// Creates the manager that reads services.json.
		/// </summary>
		/// <param name="servicesSection">Services from services.json.</param>
		/// <param name="simpleBus">Not used, kept for compatibility.</param>
		/// <param name="commandFactory">Command factory.</param>
		/// <param name="repeaterFactory">Repeater factory.</param>
		/// <param name="subscriberFactory">Subscriber factory.</param>
		/// <param name="messageHandlerFactory">Handler factory.</param>
		/// <param name="serializeService">Serializer of the local queue.</param>
		/// <param name="localLogger">Logger.</param>
		public ServiceUnitManager(IServicesSettings servicesSection,
			ISimpleBus simpleBus,
			ICommandFactory commandFactory,
			IRepeaterFactory repeaterFactory,
			ISubscriberFactory subscriberFactory,
			IMessageHandlerFactory messageHandlerFactory,
			ISerializeService serializeService,
			ILocalLogger localLogger)
		: this(servicesSection, null, simpleBus, commandFactory, repeaterFactory, subscriberFactory, messageHandlerFactory, serializeService, localLogger)
		{
		}

		/// <summary>
		/// Creates the manager with a third-party source of definitions.
		/// DI containers choose this constructor when
		/// <see cref="IServiceDefinitionSource"/> is registered.
		/// </summary>
		/// <param name="servicesSection">Services from services.json, used without a source.</param>
		/// <param name="source">Source of definitions; <c>null</c> means services.json.</param>
		/// <param name="simpleBus">Not used, kept for compatibility.</param>
		/// <param name="commandFactory">Command factory.</param>
		/// <param name="repeaterFactory">Repeater factory.</param>
		/// <param name="subscriberFactory">Subscriber factory.</param>
		/// <param name="messageHandlerFactory">Handler factory.</param>
		/// <param name="serializeService">Serializer of the local queue.</param>
		/// <param name="localLogger">Logger.</param>
		public ServiceUnitManager(IServicesSettings servicesSection,
			IServiceDefinitionSource source,
			ISimpleBus simpleBus,
			ICommandFactory commandFactory,
			IRepeaterFactory repeaterFactory,
			ISubscriberFactory subscriberFactory,
			IMessageHandlerFactory messageHandlerFactory,
			ISerializeService serializeService,
			ILocalLogger localLogger)
		{
			if (source == null && servicesSection == null)
				throw new ArgumentNullException(nameof(servicesSection));

			// Without a third-party source the units come from services.json, as before.
			_source = source ?? new ServicesJsonDefinitionSource(servicesSection);
			_commandFactory = commandFactory;
			_repeaterFactory = repeaterFactory;
			_subscriberFactory = subscriberFactory;
			_messageHandlerFactory = messageHandlerFactory;
			_logger = localLogger;

			IncreaseThreadPool();
			_internalBus = InternalBus.Create(serializeService ?? new SerializeService(), _logger);
		}

		/// <inheritdoc />
		public event UnhandledExceptionEventHandler OnError;

		/// <inheritdoc />
		public void Start()
		{
			_logger?.Info("Try start service unit");
			SyncPlan plan;
			lock (_sync)
			{
				if (_isStarted)
				{
					_logger?.Info("Already started");
					return;
				}

				_isStarted = true;
				_logger?.Info("Start load local bus");
				_internalBus.Load();
				_logger?.Info("End load local bus");
				plan = Sync(_source.Load());
			}

			_source.Changed += HandleSourceChanged;
			Execute(plan, true);
			_logger?.Info("Service unit started");
		}

		/// <inheritdoc />
		public void Stop()
		{
			_source.Changed -= HandleSourceChanged;
			List<IServiceUnit> units;
			lock (_sync)
			{
				_isStarted = false;
				units = _commands.Values
					.Concat(_handlers.Values)
					.Select(x => x.Unit)
					.Where(x => x != null)
					.ToList();
				_commands.Clear();
				_handlers.Clear();
			}

			foreach (var unit in units)
				StopUnit(unit);
		}

		/// <inheritdoc />
		public void Paused()
		{
		}

		/// <inheritdoc />
		public void Continued()
		{
		}

		/// <inheritdoc />
		public IReadOnlyList<ServiceUnitState> GetUnits()
		{
			lock (_sync)
			{
				return _handlers
					.Select(x => ToState(x.Key, ServiceUnitKind.Handler, x.Value))
					.Concat(_commands.Select(x => ToState(x.Key, ServiceUnitKind.Command, x.Value)))
					.ToList();
			}
		}

		/// <inheritdoc />
		public void AddHandler(MessageHandlerSettings settings)
		{
			ArgumentNullException.ThrowIfNull(settings);

			Add(ServiceUnitKind.Handler, settings.Name, settings);
		}

		/// <inheritdoc />
		public bool StartHandler(string name)
		{
			return Start(ServiceUnitKind.Handler, name);
		}

		/// <inheritdoc />
		public bool StopHandler(string name)
		{
			return Stop(ServiceUnitKind.Handler, name);
		}

		/// <inheritdoc />
		public bool RemoveHandler(string name)
		{
			return Remove(ServiceUnitKind.Handler, name);
		}

		/// <inheritdoc />
		public void AddCommand(CommandSettings settings)
		{
			ArgumentNullException.ThrowIfNull(settings);

			Add(ServiceUnitKind.Command, settings.Name, settings);
		}

		/// <inheritdoc />
		public bool StartCommand(string name)
		{
			return Start(ServiceUnitKind.Command, name);
		}

		/// <inheritdoc />
		public bool StopCommand(string name)
		{
			return Stop(ServiceUnitKind.Command, name);
		}

		/// <inheritdoc />
		public bool RemoveCommand(string name)
		{
			return Remove(ServiceUnitKind.Command, name);
		}

		/// <inheritdoc />
		public void Reload()
		{
			Apply(_source.Load());
		}

		private void HandleSourceChanged(object sender, ServiceDefinitionsChangedEventArgs e)
		{
			try
			{
				_logger?.Info("Service definitions changed");
				Apply(e.Definitions);
			}
			catch (Exception ex)
			{
				_logger?.Error($"Can not apply service definitions: {ex.Message}", ex);
			}
		}

		private void Apply(ServiceDefinitions definitions)
		{
			SyncPlan plan;
			lock (_sync)
			{
				if (!_isStarted) return;

				plan = Sync(definitions);
			}

			Execute(plan, false);
		}

		private void Add(ServiceUnitKind kind, string name, ISettings settings)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException($"{kind} name is required.", nameof(settings));

			var plan = new SyncPlan();
			lock (_sync)
			{
				if (!_isStarted)
					throw new InvalidOperationException("The service unit manager is not started.");

				var entries = GetEntries(kind);
				if (entries.ContainsKey(name))
					RemoveEntry(entries, name, plan);

				// A unit added at runtime overrides the source until it is removed.
				var fingerprint = SettingsFingerprint.Get(settings);
				AddEntry(kind, name, settings, fingerprint, false, true, plan);
			}

			_logger?.Info($"{kind} {name} added");
			Execute(plan, false);
		}

		private bool Start(ServiceUnitKind kind, string name)
		{
			var plan = new SyncPlan();
			lock (_sync)
			{
				if (!_isStarted || !TryGetEntry(kind, name, out var entry)) return false;

				entry.IsStoppedByUser = false;
				if (entry.Unit != null) return true;

				if (!CanRun(kind, entry.Settings))
				{
					_logger?.Warn($"{kind} {name} has degreeOfParallelism 0 and can not run");
					return false;
				}

				entry.Unit = CreateUnit(kind, entry.Settings);
				if (entry.Unit == null) return false;

				plan.Add(kind, entry.Unit);
			}

			_logger?.Info($"{kind} {name} started");
			Execute(plan, false);
			return true;
		}

		private bool Stop(ServiceUnitKind kind, string name)
		{
			IServiceUnit unit;
			lock (_sync)
			{
				if (!TryGetEntry(kind, name, out var entry)) return false;

				// Kept stopped across reloads until StartHandler/StartCommand.
				entry.IsStoppedByUser = true;
				unit = entry.Unit;
				entry.Unit = null;
			}

			if (unit != null)
				StopUnit(unit);
			_logger?.Info($"{kind} {name} stopped");
			return true;
		}

		private bool Remove(ServiceUnitKind kind, string name)
		{
			var plan = new SyncPlan();
			lock (_sync)
			{
				if (!TryGetEntry(kind, name, out _)) return false;

				RemoveEntry(GetEntries(kind), name, plan);
			}

			_logger?.Info($"{kind} {name} removed");
			Execute(plan, false);
			return true;
		}

		private SyncPlan Sync(ServiceDefinitions definitions)
		{
			var plan = new SyncPlan();
			var handlers = definitions.Handlers.Select(x => (x.Name, (ISettings)x));
			var commands = definitions.Commands.Select(x => (x.Name, (ISettings)x));
			Sync(ServiceUnitKind.Handler, ToMap(ServiceUnitKind.Handler, handlers), plan);
			Sync(ServiceUnitKind.Command, ToMap(ServiceUnitKind.Command, commands), plan);
			return plan;
		}

		private void Sync(ServiceUnitKind kind, Dictionary<string, ISettings> desired, SyncPlan plan)
		{
			var entries = GetEntries(kind);
			var removed = entries
				.Where(x => !x.Value.IsRuntime && !desired.ContainsKey(x.Key))
				.Select(x => x.Key)
				.ToList();
			foreach (var name in removed)
			{
				_logger?.Info($"{kind} {name} removed from the source");
				RemoveEntry(entries, name, plan);
			}

			foreach (var pair in desired)
			{
				var fingerprint = SettingsFingerprint.Get(pair.Value);
				entries.TryGetValue(pair.Key, out var entry);
				if (entry != null && (entry.IsRuntime || entry.Fingerprint == fingerprint))
					continue;

				var isStoppedByUser = entry?.IsStoppedByUser ?? false;
				if (entry != null)
				{
					_logger?.Info($"{kind} {pair.Key} changed in the source");
					RemoveEntry(entries, pair.Key, plan);
				}

				AddEntry(kind, pair.Key, pair.Value, fingerprint, isStoppedByUser, false, plan);
			}
		}

		private Dictionary<string, ISettings> ToMap(
			ServiceUnitKind kind,
			IEnumerable<(string Name, ISettings Settings)> items)
		{
			var result = new Dictionary<string, ISettings>(StringComparer.Ordinal);
			foreach (var (name, settings) in items)
			{
				if (string.IsNullOrWhiteSpace(name))
				{
					_logger?.Warn($"{kind} without a name is skipped");
					continue;
				}

				if (!result.TryAdd(name, settings))
					_logger?.Warn($"{kind} {name} is defined twice, the first definition is used");
			}

			return result;
		}

		private void AddEntry(
			ServiceUnitKind kind,
			string name,
			ISettings settings,
			string fingerprint,
			bool isStoppedByUser,
			bool isRuntime,
			SyncPlan plan)
		{
			var entry = new UnitEntry(settings, fingerprint, isRuntime)
			{
				IsStoppedByUser = isStoppedByUser
			};
			GetEntries(kind)[name] = entry;

			if (!CanRun(kind, settings))
			{
				// As before: a handler with 0 threads has its queue and error queue deleted.
				plan.QueuesToDelete.Add((IMessageHandlerSettings)settings);
				return;
			}

			if (isStoppedByUser) return;

			entry.Unit = CreateUnit(kind, settings);
			if (entry.Unit != null)
				plan.Add(kind, entry.Unit);
		}

		private static void RemoveEntry(Dictionary<string, UnitEntry> entries, string name, SyncPlan plan)
		{
			var unit = entries[name].Unit;
			if (unit != null)
				plan.ToStop.Add(unit);
			entries.Remove(name);
		}

		private bool TryGetEntry(ServiceUnitKind kind, string name, out UnitEntry entry)
		{
			entry = null;
			return !string.IsNullOrEmpty(name) && GetEntries(kind).TryGetValue(name, out entry);
		}

		private Dictionary<string, UnitEntry> GetEntries(ServiceUnitKind kind)
		{
			return kind == ServiceUnitKind.Handler ? _handlers : _commands;
		}

		private static bool CanRun(ServiceUnitKind kind, ISettings settings)
		{
			return kind == ServiceUnitKind.Command
				|| ((IMessageHandlerSettings)settings).DegreeOfParallelism > 0;
		}

		private IServiceUnit CreateUnit(ServiceUnitKind kind, ISettings settings)
		{
			try
			{
				IServiceUnit unit = kind == ServiceUnitKind.Handler
					? _subscriberFactory.Create((IMessageHandlerSettings)settings, _messageHandlerFactory)
					: new RepeatingCommandContainer(settings, _repeaterFactory, _commandFactory, _logger);
				unit.OnError += HandleServiceUnitError;
				return unit;
			}
			catch (Exception e)
			{
				_logger?.Error($"Can not create {kind}: {e.Message}", e);
				return null;
			}
		}

		private void Execute(SyncPlan plan, bool isInitialStart)
		{
			foreach (var unit in plan.ToStop)
				StopUnit(unit);

			foreach (var settings in plan.QueuesToDelete)
				DeleteQueues(settings);

			if (isInitialStart)
			{
				// As before: handlers start one by one, commands two at a time.
				foreach (var unit in plan.HandlersToStart)
					StartUnit(unit);
				Parallel.ForEach(
					plan.CommandsToStart,
					new ParallelOptions { MaxDegreeOfParallelism = 2 },
					StartUnit);
				return;
			}

			// Runtime changes must not block the caller while the broker is unavailable.
			foreach (var unit in plan.HandlersToStart.Concat(plan.CommandsToStart))
				unit.StartAsync();
		}

		private void StartUnit(IServiceUnit unit)
		{
			try
			{
				unit.Start();
			}
			catch (Exception e)
			{
				_logger?.Error($"Can not start {unit.Name}: {e.Message}", e);
			}
		}

		private void StopUnit(IServiceUnit unit)
		{
			try
			{
				unit.Stop();
			}
			catch (Exception e)
			{
				_logger?.Error($"Can not stop {unit.Name}: {e.Message}", e);
			}
		}

		private void DeleteQueues(IMessageHandlerSettings settings)
		{
			try
			{
				_subscriberFactory.Delete(settings);
			}
			catch (Exception e)
			{
				_logger?.Error($"Can not delete queues of {settings.Type}: {e.Message}", e);
			}
		}

		private void HandleServiceUnitError(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = (Exception)e.ExceptionObject;
			_logger?.Error(ex.InnerException?.Message ?? ex.Message, ex);
			OnError?.Invoke(sender, e);
		}

		private static ServiceUnitState ToState(string name, ServiceUnitKind kind, UnitEntry entry)
		{
			var unit = entry.Unit;
			return new ServiceUnitState(name, kind, unit != null, unit?.IsStarted ?? false);
		}

		private static void IncreaseThreadPool()
		{
			ThreadPool.GetMinThreads(out var minWorker, out var minIoc);
			ThreadPool.SetMinThreads(minWorker * 4, minIoc * 4);
			ThreadPool.GetMaxThreads(out var maxWorker, out var maxIoc);
			ThreadPool.SetMaxThreads(maxWorker * 4, maxIoc * 4);
		}

		private sealed class UnitEntry
		{
			public UnitEntry(ISettings settings, string fingerprint, bool isRuntime)
			{
				Settings = settings;
				Fingerprint = fingerprint;
				IsRuntime = isRuntime;
			}

			public ISettings Settings { get; }

			public string Fingerprint { get; }

			// Added through IServiceUnitController, the source does not remove it.
			public bool IsRuntime { get; }

			public bool IsStoppedByUser { get; set; }

			public IServiceUnit Unit { get; set; }
		}

		private sealed class SyncPlan
		{
			public List<IServiceUnit> ToStop { get; } = new List<IServiceUnit>();

			public List<IMessageHandlerSettings> QueuesToDelete { get; } = new List<IMessageHandlerSettings>();

			public List<IServiceUnit> HandlersToStart { get; } = new List<IServiceUnit>();

			public List<IServiceUnit> CommandsToStart { get; } = new List<IServiceUnit>();

			public void Add(ServiceUnitKind kind, IServiceUnit unit)
			{
				if (kind == ServiceUnitKind.Handler)
					HandlersToStart.Add(unit);
				else
					CommandsToStart.Add(unit);
			}
		}
	}
}
