namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpResponse
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("result")]
	public JsonElement? Result { get; set; }

	[JsonPropertyName("error")]
	public CdpError? Error { get; set; }
}
