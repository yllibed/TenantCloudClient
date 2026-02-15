namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpGetCookiesResult
{
	[JsonPropertyName("cookies")]
	public CdpCookie[] Cookies { get; set; } = [];
}
