namespace Bsa.Msa.RabbitMq.Core
{
	/// <summary>
	/// Provides serialization and deserialization services for converting objects to strings and vice versa.
	/// Typically used for data interchange formats like JSON, XML, or others.
	/// </summary>
	public interface ISerializeService
	{
		/// <summary>
		/// Deserializes a string representation into an object of type <typeparamref name="TValue"/>.
		/// </summary>
		/// <typeparam name="TValue">The type of the object to deserialize to.</typeparam>
		/// <param name="value">The string value containing the serialized data.</param>
		/// <returns>The deserialized object of type <typeparamref name="TValue"/>.</returns>
		TValue Deserialize<TValue>(string value);

		/// <summary>
		/// Serializes the given object into a string representation.
		/// </summary>
		/// <param name="obj">The object to serialize.</param>
		/// <returns>A string containing the serialized representation of the object.</returns>
		string Serialize(object obj);
	}
}
