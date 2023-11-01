using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Bsa.Msa.Common.Serialization
{
	/// <summary>
	/// Represents the XML serializer.
	/// </summary>
	public class XmlSerializer : ISerializer
	{
		/// <summary>
		/// Serializes the object to the byte array.
		/// </summary>
		/// <param name="type">The type of the object</param>
		/// <param name="serializingObject">The object to serialize</param>
		/// <returns>Serialized data as the byte array</returns>
		public byte[] Serialize(Type type, object serializingObject)
		{
			if (serializingObject == null)
			{
				return new byte[] { };
			}

			try
			{
				var writerSettings = new XmlWriterSettings()
				{
					CheckCharacters = false,
					NewLineHandling = NewLineHandling.Entitize
				};

				using (var stream = new MemoryStream())
				using (var writer = XmlWriter.Create(stream, writerSettings))
				{
					var serializer = new System.Xml.Serialization.XmlSerializer(type);
					serializer.Serialize(writer, serializingObject);
					return stream.ToArray();
				}
			}
			catch (Exception e)
			{
				throw new SerializationException(string.Format("The error during message serializing. type={0} object={1}", type, serializingObject), e);
			}
		}

		/// <summary>
		/// Deserializes the byte array to the object of the given type.
		/// </summary>
		/// <param name="type">The type of the object</param>
		/// <param name="serializedData">The serialized data as byte array</param>
		/// <returns>The deserialized object of the given type</returns>
		public object Deserialize(Type type, byte[] serializedData)
		{
			if (serializedData == null)
			{
				throw new ArgumentNullException("serializedData");
			}
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}


			if (serializedData.Length == 0)
			{
				return null;
			}

			var xmlReaderSettings = new XmlReaderSettings()
			{
				CheckCharacters = false
			};

			try
			{
				using (var stream = new MemoryStream(serializedData))
				using (var reader = XmlReader.Create(stream, xmlReaderSettings))
				{
					var serializer = new System.Xml.Serialization.XmlSerializer(type);
					var message = serializer.Deserialize(reader);
					return message;
				}
			}
			catch (Exception e)
			{
				throw new SerializationException(string.Format("The error during message deserializing. type={0} object={1}", type, serializedData), e);
			}
		}
	}
}
