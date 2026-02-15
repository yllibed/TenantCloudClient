namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpRemoteObject
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("value")]
	public object? Value { get; set; }
}
