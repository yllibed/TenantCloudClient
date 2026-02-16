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
		writer.WriteStringValue(value switch
		{
			TcTransactionStatus.Due => "due",
			TcTransactionStatus.Paid => "paid",
			TcTransactionStatus.Partial => "partial",
			TcTransactionStatus.Pending => "pending",
			TcTransactionStatus.Void => "void",
			TcTransactionStatus.WithBalance => "with_balance",
			TcTransactionStatus.Overdue => "overdue",
			TcTransactionStatus.Waive => "waive",
			_ => value.ToString().ToLowerInvariant(),
		});
	}
}
