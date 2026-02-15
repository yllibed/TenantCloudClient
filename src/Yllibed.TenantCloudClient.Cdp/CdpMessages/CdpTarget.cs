namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpTarget
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "";

	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("title")]
	public string Title { get; set; } = "";

	[JsonPropertyName("url")]
	public string Url { get; set; } = "";

	[JsonPropertyName("webSocketDebuggerUrl")]
	public string WebSocketDebuggerUrl { get; set; } = "";
}
