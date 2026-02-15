namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class TcRefreshResponse
{
	[JsonPropertyName("access_token")]
	public string? AccessToken { get; set; }

	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; set; }

	[JsonPropertyName("token_type")]
	public string? TokenType { get; set; }

	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; set; }

	[JsonPropertyName("user_id")]
	public int UserId { get; set; }
}
