using System;
using System.Collections.Generic;

namespace Bsa.Msa.Common.Services.Interfaces
{
	/// <summary>
	/// Maps short and full type names to values. A short name shared
	/// by several types is ambiguous and resolves only by the full name;
	/// before, the last found type silently replaced the others.
	/// </summary>
	/// <typeparam name="TValue">Stored value.</typeparam>
	internal sealed class TypeNameMap<TValue> where TValue : class
	{
		private readonly Dictionary<string, TValue> _values =
			new Dictionary<string, TValue>(StringComparer.Ordinal);

		private readonly Dictionary<string, Type> _owners =
			new Dictionary<string, Type>(StringComparer.Ordinal);

		private readonly HashSet<string> _ambiguousNames =
			new HashSet<string>(StringComparer.Ordinal);

		/// <summary>
		/// Adds a found type by its short and full names.
		/// </summary>
		/// <param name="type">Found type.</param>
		/// <param name="value">Value for the type.</param>
		public void Add(Type type, TValue value)
		{
			var fullName = type.FullName ?? type.Name;
			_values[fullName] = value;

			var name = type.Name;
			if (name == fullName || _ambiguousNames.Contains(name))
				return;

			if (_owners.TryGetValue(name, out var owner) && owner != type)
			{
				_values.Remove(name);
				_ambiguousNames.Add(name);
				return;
			}

			_owners[name] = type;
			_values[name] = value;
		}

		/// <summary>
		/// Sets a value by an explicit name, it wins over found types.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Set(string name, TValue value)
		{
			_values[name] = value;
			_ambiguousNames.Remove(name);
		}

		/// <summary>
		/// Finds a value by a short or full name.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <returns>Value or <c>null</c> if the name is unknown.</returns>
		public TValue Find(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;

			if (_values.TryGetValue(name, out var value)) return value;

			if (_ambiguousNames.Contains(name))
			{
				throw new InvalidOperationException(
					$"Type name '{name}' is ambiguous, use the full type name.");
			}

			return null;
		}
	}
}
