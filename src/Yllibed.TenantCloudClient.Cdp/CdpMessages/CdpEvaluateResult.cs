namespace Yllibed.TenantCloudClient.Cdp.CdpMessages;

internal sealed class CdpEvaluateResult
{
	[JsonPropertyName("result")]
	public CdpRemoteObject? Result { get; set; }

	[JsonPropertyName("exceptionDetails")]
	public CdpExceptionDetails? ExceptionDetails { get; set; }
}
