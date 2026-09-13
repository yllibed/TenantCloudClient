namespace Yllibed.TenantCloudClient.HttpMessages;

/// <summary>
/// Reads a JSON value that TenantCloud sometimes serializes as a string and
/// sometimes as a number (observed on <c>property_status</c>) into a string.
/// Writes back out as a string.
/// </summary>
public class JsonFlexibleStringConverter : JsonConverter<string?>
{
	public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.String:
				return reader.GetString();
			case JsonTokenType.Number:
				using (var value = JsonDocument.ParseValue(ref reader))
				{
					return value.RootElement.GetRawText();
				}
			case JsonTokenType.True:
				return "true";
			case JsonTokenType.False:
				return "false";
			case JsonTokenType.Null:
				return null;
			default:
				throw new JsonException($"Type {reader.TokenType} not supported for flexible string");
		}
	}

	public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNullValue();
		}
		else
		{
			writer.WriteStringValue(value);
		}
	}
}
