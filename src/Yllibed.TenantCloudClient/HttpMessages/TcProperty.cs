using System;
using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcProperty
	{
		[JsonPropertyName("id")]
		[JsonConverter(typeof(JsonAutoLongConverter))]
		public long Id { get; set; }

		public string? Name => Attributes?.Name;

		public string Address => $"{Attributes?.Address1} {Attributes?.CityAddress}";

		[JsonPropertyName("attributes")]
		public TcPropertyAttributes? Attributes { get; set; }
	}
}
