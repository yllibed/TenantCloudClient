namespace Yllibed.TenantCloudClient.HttpMessages;

public class JsonTcTransactionStatusConverter : JsonConverter<TcTransactionStatus>
{
	public override TcTransactionStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Number:
				return (TcTransactionStatus)reader.GetByte();
			case JsonTokenType.String:
				var str = reader.GetString();
				if (Enum.TryParse<TcTransactionStatus>(str, true, out var result))
				{
					return result;
				}

				throw new NotSupportedException($"Unknown status {str}");
			default:
				throw new NotSupportedException($"Type {reader.TokenType} not supported");
		}
	}

	public override void Write(Utf8JsonWriter writer, TcTransactionStatus value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue((byte)value);
	}
}
