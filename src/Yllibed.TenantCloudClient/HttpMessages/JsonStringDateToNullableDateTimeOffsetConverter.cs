using System.Globalization;

namespace Yllibed.TenantCloudClient.HttpMessages;

public class JsonStringDateToNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
	public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var str = reader.GetString();
		if (str is null)
		{
			return null;
		}

		if (DateFormats.TryParse(str, out var dto))
		{
			return dto;
		}

		throw new NotSupportedException("Unknown Date format for " + str);
	}

	public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		writer.WriteStringValue(value.Value.ToString("M/d/yyyy", CultureInfo.InvariantCulture));
	}
}
