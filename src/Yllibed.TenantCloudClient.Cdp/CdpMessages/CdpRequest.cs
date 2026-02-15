namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpRequest
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("method")]
	public string Method { get; set; } = "";

	[JsonPropertyName("params")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public JsonElement? Params { get; set; }
}
