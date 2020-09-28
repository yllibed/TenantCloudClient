using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class JsonDecimalConverter : JsonConverter<decimal>
	{
		public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Number)
			{
				return reader.GetDecimal();
			}
			throw new NotSupportedException();
		}

		public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
		{
			throw new NotSupportedException();
		}
	}
}
