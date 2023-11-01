using System.Collections.Generic;
using System.Xml.Linq;
using Bsa.Msa.Common.Settings;
using Bsa.Msa.Common.Services.Commands;
using Bsa.Msa.Common.Services.MessageHandling;
using Microsoft.Extensions.Configuration;

namespace Bsa.Msa.Common.Services.Settings
{
	/// <summary>
	/// Конфигурационная секция с информацией о всех логических службах
	/// </summary>
	public class ServiceSettings : SettingsConfigurationSection
	{

		public string Postfix { get; set; }
		public string Name { get; protected set; }


		private readonly Dictionary<string, MessageHandlerSettings> _handlerSections = new Dictionary<string, MessageHandlerSettings>();
		private readonly Dictionary<string, CommandSettings> _operationSections = new Dictionary<string, CommandSettings>();
		public ServiceSettings(string name, IConfigurationSection raw)
			: base(raw)
		{
			Name = name;
			Load(raw);
		}

		private void Load(IConfigurationSection raw)
		{
			LoadHandlers(raw);
			LoadOperations(raw);
		}

		private void LoadHandlers(IConfigurationSection raw)
		{
			Postfix = raw.GetSection("postfix")?.Value;
			var handler = raw.GetSection("handlers");
			if (handler == null)
				return;
			var rawHandlers = handler.GetChildren();
			foreach (var handlerItem in rawHandlers)
			{
				var attributeName = handlerItem.GetSection("name");

				if (attributeName != null)
				{
					var нandlerSection = new MessageHandlerSettings(attributeName.Value, Postfix, handlerItem);
					if (!_handlerSections.ContainsKey(attributeName.Value))
						_handlerSections.Add(attributeName.Value, нandlerSection);
				}
			}
		}

		private void LoadOperations(IConfigurationSection raw)
		{
			var commands = raw.GetSection("commands");
			if (commands == null)
				return;
			var rawOperations = commands.GetChildren();
			foreach (var xItem in rawOperations)
			{
				var attributeName = xItem.GetSection("name");
				if (attributeName != null)
				{
					var нandlerSection = new CommandSettings(attributeName.Value, xItem);
					if (!_operationSections.ContainsKey(attributeName.Value))
						_operationSections.Add(attributeName.Value, нandlerSection);
				}
			}
		}

		/// <summary>
		/// Список обработчиков
		/// </summary>
		/// <returns></returns>
		public IEnumerable<MessageHandlerSettings> GetHandlers()
		{
			return _handlerSections.Values;
		}

		/// <summary>
		/// Список операций
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CommandSettings> GetCommands()
		{
			return _operationSections.Values;
		}
	}
}
