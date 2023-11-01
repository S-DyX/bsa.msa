using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Bsa.Msa.Common.Services.Interfaces
{
	public static class AssemblyHelpers
	{
		public static List<Type> GetDeliveredTypes(this Type type)
		{
			var typesList = new List<Type>();

			var assenblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (var assembly in assenblies)
			{
				var commands = assembly.GetTypes();
				foreach (var t in commands)
				{

					if (!type.IsAssignableFrom(t))
					{
						continue;
					}
					typesList.Add(t);

				}
			}
			var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

			foreach (var file in files)
			{
				try
				{

					var assembly = Assembly.LoadFile(file);
					if (assenblies.Contains(assembly))
						continue;
					var collection = assembly.GetTypes();
					foreach (var p in collection)
					{
						if (type.IsAssignableFrom(p))
						{
							typesList.Add(p);
						}
					}

				}
				catch (Exception e)
				{
				}
			}

			return typesList;
		}
	}
}
