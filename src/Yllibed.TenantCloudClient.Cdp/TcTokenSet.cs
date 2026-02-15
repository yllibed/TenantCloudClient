using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.Cdp;

/// <summary>
/// Holds the three pieces required for TenantCloud API authentication and token refresh.
/// </summary>
public sealed record TcTokenSet(
	[property: JsonPropertyName("access_token")] string AccessToken,
	[property: JsonPropertyName("refresh_token")] string RefreshToken,
	[property: JsonPropertyName("fingerprint")] string Fingerprint);
