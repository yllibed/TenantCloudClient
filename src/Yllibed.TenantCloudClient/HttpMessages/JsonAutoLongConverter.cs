using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class JsonAutoLongConverter : JsonConverter<long>
	{
		public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.Number:
					return reader.GetInt64();
				case JsonTokenType.String:
					var str = reader.GetString();
					return long.Parse(str, NumberFormatInfo.InvariantInfo);
				default:
					throw new NotSupportedException($"Type {reader.TokenType} not supported");
			}
		}

		public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
		{
			throw new NotSupportedException();
		}
	}
	public class JsonAutoNullableLongConverter : JsonConverter<long?>
	{
		public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			switch (reader.TokenType)
			{
				case JsonTokenType.Null:
					return null;
				case JsonTokenType.Number:
					return reader.GetInt64();
				case JsonTokenType.String:
					var str = reader.GetString();
					return long.Parse(str, NumberFormatInfo.InvariantInfo);
				default:
					throw new NotSupportedException($"Type {reader.TokenType} not supported");
			}
		}

		public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
		{
			throw new NotSupportedException();
		}
	}
}
