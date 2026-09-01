using System.Globalization;

namespace Yllibed.TenantCloudClient.HttpMessages;

/// <summary>
/// Reads a JSON decimal value that may also legitimately be <c>null</c> or a
/// numeric string (observed on unit <c>price</c>). Returns <c>null</c> for a
/// JSON null and parses strings tolerantly.
/// </summary>
public class JsonNullableDecimalConverter : JsonConverter<decimal?>
{
	public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Null:
				return null;
			case JsonTokenType.Number:
				return reader.GetDecimal();
			case JsonTokenType.String:
				var str = reader.GetString();
				return string.IsNullOrWhiteSpace(str) ? null : decimal.Parse(str, CultureInfo.InvariantCulture);
			default:
				throw new NotSupportedException($"Type {reader.TokenType} not supported for nullable decimal");
		}
	}

	public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNullValue();
		}
		else
		{
			writer.WriteNumberValue(value.Value);
		}
	}
}
