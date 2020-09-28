using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class JsonStringToEnumConverter<T> : JsonConverter<T>
	{
		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (typeToConvert.IsEnum)
			{
				var str = reader.GetString();
				return (T)Enum.Parse(typeToConvert, str, true);
			}
			throw new NotSupportedException();
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			throw new NotSupportedException();
		}
	}
}
