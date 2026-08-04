using System.Text.Json;

namespace Bsa.Msa.RabbitMq.Core
{
	/// <inheritdoc />
	public sealed class SerializeService : ISerializeService
	{
		/// <inheritdoc />
		public TValue Deserialize<TValue>(string value)
		{
			return JsonSerializer.Deserialize<TValue>(value);
		}

		/// <inheritdoc />
		public string Serialize(object obj)
		{
			return JsonSerializer.Serialize(obj);
		}
	}
}
