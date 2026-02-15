namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class TcRefreshRequest
{
	[JsonPropertyName("grant_type")]
	public string GrantType { get; set; } = "refresh_token";

	[JsonPropertyName("fingerprint")]
	public string Fingerprint { get; set; } = "";

	[JsonPropertyName("refresh_token")]
	public string RefreshToken { get; set; } = "";
}
