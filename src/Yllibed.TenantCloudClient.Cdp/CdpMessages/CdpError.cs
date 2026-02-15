namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpError
{
	[JsonPropertyName("code")]
	public int Code { get; set; }

	[JsonPropertyName("message")]
	public string Message { get; set; } = "";
}
