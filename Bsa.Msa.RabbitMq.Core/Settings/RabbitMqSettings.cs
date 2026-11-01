using Bsa.Msa.Common.Settings;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Bsa.Msa.RabbitMq.Core.Settings
{
	/// <inheritdoc cref="IRabbitMqSettings" />
	public sealed class RabbitMqSettings : SettingsBase, IRabbitMqSettings
	{
		protected override string SectionName
		{
			get
			{
				return "rabbitMQ";
			}
		}

		/// <summary>
		/// Ctor
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		public RabbitMqSettings()
		{
			// Set before loading: the section may override it with "port".
			Port = DefaultPort;
			try
			{

				LoadSettings(GetElement());
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("section not found or has bad format in appsettings.json  \"rabbitMq\": {\r\n    \"host\": \"localhost\",\r\n    \"username\": \"guest\",\r\n    \"password\": \"guest\",\r\n    //\"virtualHost\": \"virtual\",\r\n    \"timeout\": 10\r\n  }");
			}
		}
		private Random _rnd = new Random(2);

		private const int DefaultPort = 5672;

		private void LoadSettings(IConfigurationSection raw)
		{
			if (raw == null)
				throw new InvalidDataException($"Section not found:{SectionName}");

			this.Name = raw.GetSection("name") != null ? raw.GetSection("name")?.Value : string.Empty;
			this.UserName = raw.GetSection("username")?.Value ?? "guest";
			this.Password = raw.GetSection("password")?.Value ?? "guest";
			this.VirtualHost = raw.GetSection("virtualHost")?.Value;
			var port = raw.GetSection("port")?.Value;
			if (!string.IsNullOrEmpty(port)) Port = int.Parse(port, CultureInfo.InvariantCulture);
			Hosts = raw.GetSection("host").Value.Split(';').ToList();
			_rnd = new Random(Hosts?.Count ?? 0);
			////this.Host = raw.Attribute("host").Value;
			//var timeout = raw.Attribute("timeout");
			//this.Timeout = timeout != null ? int.Parse(timeout.Value) : 60;


		}

		/// <inheritdoc cref="IRabbitMqSettings" />
		public RabbitMqSettings(string connectionString)
		{
			Port = DefaultPort;
			Hosts = new List<string>();
			var values = connectionString.Split(';');
			foreach (var v in values)
			{
				var keyValue = v.Split('=');
				switch (keyValue[0])
				{
					case "host":
						Hosts.Add(keyValue[1]);
						break;
					case "username":
						UserName = keyValue[1];
						break;
					case "password":
						Password = keyValue[1];
						break;
					case "timeout":
						Timeout = int.Parse(keyValue[1]);
						break;
					case "port":
						Port = int.Parse(keyValue[1], CultureInfo.InvariantCulture);
						break;
					case "virtualHost":
						if (!string.IsNullOrEmpty(keyValue[1]))
							VirtualHost = keyValue[1];
						break;
				}
			}
		}

		/// <inheritdoc cref="IRabbitMqSettings" />
		public IConfigurationSection Raw { get; protected set; }

		/// <inheritdoc cref="IRabbitMqSettings" />
		public string Name
		{
			get;
			protected set;
		}

		/// <inheritdoc cref="IRabbitMqSettings" />
		public string UserName
		{
			get;
			protected set;
		}

		/// <inheritdoc cref="IRabbitMqSettings" />
		public string Password
		{
			get;
			protected set;
		}

		/// <inheritdoc cref="IRabbitMqSettings" />
		public string Host
		{
			get
			{
				if (Hosts.Count == 1)
					return Hosts.FirstOrDefault();

				return Hosts[_rnd.Next(0, Hosts.Count)];

			}

		}

		/// <inheritdoc cref="IRabbitMqSettings" />
		public string VirtualHost { get; private set; }

		private List<string> Hosts
		{
			get;
			set;
		}

		/// <inheritdoc cref="IRabbitMqSettings" />
		public int Port
		{
			get;
			protected set;
		}

		/// <inheritdoc cref="IRabbitMqSettings" />
		public int PrefetchCount
		{
			get;
			protected set;
		}

		/// <inheritdoc cref="IRabbitMqSettings" />
		public int Timeout
		{
			get;
			protected set;
		}


		/// <inheritdoc cref="IRabbitMqSettings" />
		public string ConnectionString
		{
			get
			{
				return string.Format(
					CultureInfo.InvariantCulture,
					"host={0};port={1};username={2};password={3};timeout={4};virtualHost={5}",
					Host,
					Port,
					UserName,
					Password,
					Timeout,
					VirtualHost);
			}
		}
	}
}
