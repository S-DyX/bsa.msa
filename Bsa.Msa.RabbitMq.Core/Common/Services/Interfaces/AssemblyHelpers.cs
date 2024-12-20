using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
				try
				{
					var name = assembly.GetName().Name;
					if (!string.IsNullOrEmpty(name)
						&& (name.StartsWith("microsoft")
						 || name.StartsWith("system")))
						continue;

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
				catch (Exception e)
				{
				}
			}
			LoadFiles(type, assenblies, typesList);

			return typesList;
		}

		private static void LoadFiles(Type type, Assembly[] assenblies, List<Type> typesList)
		{
			var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");

			foreach (var file in files)
			{
				try
				{
					if (file.ToLower().StartsWith("microsoft"))
						continue;
					if (file.ToLower().StartsWith("system"))
						continue;
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
		}
	}
}
