using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Bsa.Msa.Common.Services.Interfaces
{
	/// <summary>
	/// Finds types in the loaded assemblies and in the assemblies
	/// of the application folder.
	/// </summary>
	public static class AssemblyHelpers
	{
		private static readonly string[] SkippedPrefixes =
		{
			"Microsoft",
			"System",
			"netstandard",
			"mscorlib"
		};

		/// <summary>
		/// Returns the types assignable to <paramref name="type"/>.
		/// </summary>
		/// <param name="type">Base type or interface.</param>
		/// <returns>Found types.</returns>
		public static List<Type> GetDeliveredTypes(this Type type)
		{
			ArgumentNullException.ThrowIfNull(type);

			return GetTypes(type.IsAssignableFrom);
		}

		/// <summary>
		/// Returns the types that match <paramref name="predicate"/>.
		/// </summary>
		/// <param name="predicate">Type filter.</param>
		/// <returns>Found types without duplicates.</returns>
		public static List<Type> GetTypes(Func<Type, bool> predicate)
		{
			ArgumentNullException.ThrowIfNull(predicate);

			return GetAssemblies()
				.SelectMany(GetLoadableTypes)
				.Where(x => IsMatch(predicate, x))
				.Distinct()
				.ToList();
		}

		private static List<Assembly> GetAssemblies()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(x => !x.IsDynamic && !IsSkipped(x.GetName().Name))
				.ToList();
			var names = new HashSet<string>(
				AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().Name),
				StringComparer.OrdinalIgnoreCase);

			var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
			foreach (var file in files)
			{
				// An assembly is loaded once, into the default context:
				// Assembly.LoadFile created a second copy of loaded types.
				var name = Path.GetFileNameWithoutExtension(file);
				if (IsSkipped(name) || names.Contains(name))
					continue;

				var assembly = TryLoad(file);
				if (assembly != null && names.Add(assembly.GetName().Name))
					assemblies.Add(assembly);
			}

			return assemblies;
		}

		private static Assembly TryLoad(string file)
		{
			try
			{
				return AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
			}
			catch (Exception)
			{
				// Not a managed assembly or already loaded from another path.
				return null;
			}
		}

		private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				// Types with missing dependencies are skipped, the rest is used.
				return ex.Types.Where(x => x != null);
			}
			catch (Exception)
			{
				return Type.EmptyTypes;
			}
		}

		private static bool IsMatch(Func<Type, bool> predicate, Type type)
		{
			try
			{
				return predicate(type);
			}
			catch (Exception)
			{
				// GetInterfaces and similar calls fail on types with missing dependencies.
				return false;
			}
		}

		private static bool IsSkipped(string name)
		{
			if (string.IsNullOrEmpty(name)) return true;

			return SkippedPrefixes.Any(
				x => name.StartsWith(x, StringComparison.OrdinalIgnoreCase));
		}
	}
}
