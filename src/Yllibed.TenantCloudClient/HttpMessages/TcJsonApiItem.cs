using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages;

internal class TcJsonApiItem<T>
{
	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("id")]
	[JsonConverter(typeof(JsonAutoLongConverter))]
	public long Id { get; set; }

	[JsonPropertyName("attributes")]
	public T? Attributes { get; set; }
}
