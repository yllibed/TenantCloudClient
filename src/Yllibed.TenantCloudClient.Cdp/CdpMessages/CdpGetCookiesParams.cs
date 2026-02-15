using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpGetCookiesParams
{
	[JsonPropertyName("urls")]
	public string[] Urls { get; set; } = [];
}
