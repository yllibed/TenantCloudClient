using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class JsonStringDateToDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
	{
		private static readonly string[] _formats = new[]
		{
			"M/d/yyyy",
			"MM/dd/yyyy",
			"MM/d/yyyy",
			"M/dd/yyyy"
		};

		public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var str = reader.GetString();
			if (DateTimeOffset.TryParseExact(str, _formats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var dto))
			{
				return dto;
			}
			throw new NotSupportedException("Unknown Date format for " + str);
		}

		public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
		{
			throw new NotSupportedException();
		}
	}
}
