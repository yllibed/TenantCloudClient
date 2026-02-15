using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpExceptionDetails
{
	[JsonPropertyName("text")]
	public string Text { get; set; } = "";
}
