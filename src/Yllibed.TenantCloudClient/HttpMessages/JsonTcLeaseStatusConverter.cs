namespace Yllibed.TenantCloudClient.HttpMessages;

public class JsonTcLeaseStatusConverter : JsonConverter<TcLeaseStatus>
{
	public override TcLeaseStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.Number:
				return (TcLeaseStatus)reader.GetByte();
			case JsonTokenType.String:
				var str = reader.GetString();
				if (Enum.TryParse<TcLeaseStatus>(str, true, out var result))
				{
					return result;
				}

				throw new NotSupportedException($"Unknown lease status {str}");
			default:
				throw new NotSupportedException($"Type {reader.TokenType} not supported");
		}
	}

	public override void Write(Utf8JsonWriter writer, TcLeaseStatus value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value switch
		{
			TcLeaseStatus.Active => "active",
			TcLeaseStatus.Archived => "archived",
			TcLeaseStatus.Ended => "ended",
			TcLeaseStatus.Expired => "expired",
			TcLeaseStatus.ExpiresIn => "expires_in",
			TcLeaseStatus.Future => "future",
			TcLeaseStatus.InsurancePending => "insurance_pending",
			TcLeaseStatus.NotActive => "not_active",
			TcLeaseStatus.Pending => "pending",
			_ => value.ToString().ToLowerInvariant(),
		});
	}
}
