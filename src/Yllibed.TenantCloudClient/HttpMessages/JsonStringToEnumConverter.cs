using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages;

public class JsonStringToEnumConverter<T> : JsonConverter<T>
	where T : struct, Enum
{
	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var str = reader.GetString()
			?? throw new NotSupportedException("Null string token not supported");
		return Enum.Parse<T>(str, true);
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString().ToLowerInvariant());
	}
}
