using System;

namespace Bsa.Msa.Common.Serialization
{
	public interface ISerializer
	{
		byte[] Serialize(Type type, object serializingObject);

		object Deserialize(Type type, byte[] serializedData);
	}
}
