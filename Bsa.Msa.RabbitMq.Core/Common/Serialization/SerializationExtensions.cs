using System;
using System.Linq;

namespace Bsa.Msa.Common.Serialization
{
	/// <summary>
	/// Represents the serialization extensions.
	/// </summary>
	public static class SerializationExtensions
	{
		/// <summary>
		/// Serializes the object of the TObject type to the byte array.
		/// </summary>
		/// <typeparam name="TObject">The type of the object to serilize</typeparam>
		/// <param name="serializer">The serializer.</param>
		/// <param name="value">The object to serialize</param>
		/// <returns>Serialized data as the byte array</returns>
		public static byte[] Serialize<TObject>(this ISerializer serializer, TObject value)
		{
			return serializer.Serialize(typeof(TObject), value);
		}

		/// <summary>
		/// Deserializes the byte array to the object of the TObject type.
		/// </summary>
		/// <typeparam name="TObject">The type of the object to deserialize</typeparam>
		/// <param name="serializer">The serializer.</param>
		/// <param name="serializedData">The serialized data as byte array</param>
		/// <returns>The deserialized object of the TObject type</returns>
		public static TObject Deserialize<TObject>(this ISerializer serializer, byte[] serializedData)
		{
			return (TObject)serializer.Deserialize(typeof(TObject), serializedData);
		}

		/// <summary>
		/// Serializes the object of the given type to the string.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <param name="type">The type of the object</param>
		/// <param name="value">The object to serialize</param>
		/// <returns>Serialized data as string</returns>
		public static string SerializeToString(this ISerializer serializer, Type type, object value)
		{
			return Convert.ToBase64String(serializer.Serialize(type, value));
		}

		/// <summary>
		/// Deserializes the string to the object.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <param name="type">The type of the object</param>
		/// <param name="serializedData">The serialized data as string</param>
		/// <returns>The deserialized object of <paramref name="type"/> type</returns>
		public static object DeserializeFromString(this ISerializer serializer, Type type, string serializedData)
		{
			return serializer.Deserialize(type, Convert.FromBase64String(serializedData));
		}

		public static object DeserializeFromString(this ISerializer serializer, string messageType, string serializedData)
		{
			var type = Type.GetType(messageType);
			if (type == null)
			{
				var typeName = messageType.Split(',').FirstOrDefault();
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Reverse())
				{
					type = assembly.GetType(typeName);
					if (type != null)
					{
						break;
					}
				}
			}
			return serializer.Deserialize(type, Convert.FromBase64String(serializedData));
		}
	}
}
