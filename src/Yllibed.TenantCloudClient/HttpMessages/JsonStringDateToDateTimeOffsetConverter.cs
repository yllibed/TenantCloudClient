using System.Globalization;

namespace Yllibed.TenantCloudClient.HttpMessages;

public class JsonStringDateToDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
	private static readonly string[] Formats =
	[
		"M/d/yyyy",
		"MM/dd/yyyy",
		"MM/d/yyyy",
		"M/dd/yyyy",
	];

	public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var str = reader.GetString();
		if (DateTimeOffset.TryParseExact(str, Formats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var dto))
		{
			return dto;
		}

		throw new NotSupportedException("Unknown Date format for " + str);
	}

	public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString("M/d/yyyy", CultureInfo.InvariantCulture));
	}
}
