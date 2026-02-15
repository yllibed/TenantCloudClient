using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpCookie
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("value")]
	public string Value { get; set; } = "";

	[JsonPropertyName("domain")]
	public string Domain { get; set; } = "";

	[JsonPropertyName("httpOnly")]
	public bool HttpOnly { get; set; }

	[JsonPropertyName("secure")]
	public bool Secure { get; set; }
}
