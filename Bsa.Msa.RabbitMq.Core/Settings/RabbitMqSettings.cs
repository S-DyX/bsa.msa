using Bsa.Msa.Common.Settings;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bsa.Msa.RabbitMq.Core.Settings
{

	public sealed class RabbitMqSettings : SettingsBase, IRabbitMqSettings
	{
		protected override string SectionName
		{
			get
			{
				return "rabbitMQ";
			}
		}
		public RabbitMqSettings()
		{
			try
			{

				LoadSettings(GetElement());
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("section not found or has bad format in appsettings.json  \"rabbitMq\": {\r\n    \"host\": \"localhost\",\r\n    \"username\": \"guest\",\r\n    \"password\": \"guest\",\r\n    //\"virtualHost\": \"virtual\",\r\n    \"timeout\": 10\r\n  }");
			}
			Port = 5672;
		}
		private Random _rnd = new Random(2);

		private void LoadSettings(IConfigurationSection raw)
		{
			if (raw == null)
				throw new InvalidDataException($"Section not found:{SectionName}");

			this.Name = raw.GetSection("name") != null ? raw.GetSection("name")?.Value : string.Empty;
			this.UserName = raw.GetSection("username")?.Value ?? "guest";
			this.Password = raw.GetSection("password")?.Value ?? "guest";
			this.VirtualHost = raw.GetSection("virtualHost")?.Value;
			Hosts = raw.GetSection("host").Value.Split(';').ToList();
			_rnd = new Random(Hosts?.Count ?? 0);
			////this.Host = raw.Attribute("host").Value;
			//var timeout = raw.Attribute("timeout");
			//this.Timeout = timeout != null ? int.Parse(timeout.Value) : 60;


		}

		public RabbitMqSettings(string connectionString)
		{
			Port = 5672;
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
					case "virtualHost":
						if (!string.IsNullOrEmpty(keyValue[1]))
							VirtualHost = keyValue[1];
						break;
				}
			}
		}


		public IConfigurationSection Raw { get; protected set; }

		public string Name
		{
			get;
			protected set;
		}

		public string UserName
		{
			get;
			protected set;
		}

		public string Password
		{
			get;
			protected set;
		}

		public string Host
		{
			get
			{
				if (Hosts.Count == 1)
					return Hosts.FirstOrDefault();

				return Hosts[_rnd.Next(0, Hosts.Count)];

			}

		}

		public string VirtualHost { get; private set; }

		private List<string> Hosts
		{
			get;
			set;
		}
		public int Port
		{
			get;
			protected set;
		}

		public int PrefetchCount
		{
			get;
			protected set;
		}

		public int Timeout
		{
			get;
			protected set;
		}



		public string ConnectionString
		{
			get
			{
				return string.Format("host={0};username={1};password={2};timeout={3};virtualHost={4}", Host, UserName, Password, this.Timeout, VirtualHost);
			}
		}
	}
}
