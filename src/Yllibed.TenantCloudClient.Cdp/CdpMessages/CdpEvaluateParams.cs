using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpEvaluateParams
{
	[JsonPropertyName("expression")]
	public string Expression { get; set; } = "";

	[JsonPropertyName("returnByValue")]
	public bool ReturnByValue { get; set; } = true;
}
