namespace Yllibed.TenantCloudClient.Cdp;

/// <summary>
/// Configuration for <see cref="CdpTokenProvider"/>.
/// </summary>
public sealed class CdpTokenProviderOptions
{
	/// <summary>CDP debug port to connect to an existing browser.</summary>
	public int DebugPort { get; init; } = 9222;

	/// <summary>When true, the provider may launch a browser for interactive login.</summary>
	public bool AllowInteractiveLogin { get; init; } = false;

	/// <summary>TenantCloud web application URL.</summary>
	public string TenantCloudAppUrl { get; init; } = "https://app.tenantcloud.com";

	/// <summary>TenantCloud API URL.</summary>
	public string TenantCloudApiUrl { get; init; } = "https://api.tenantcloud.com";

	/// <summary>Optional token store for persisting tokens across sessions.</summary>
	public ITcTokenStore? TokenStore { get; init; }

	/// <summary>Override automatic browser discovery with a specific executable path.</summary>
	public string? BrowserExecutablePath { get; init; }

	/// <summary>Timeout for CDP WebSocket operations.</summary>
	public TimeSpan WebSocketTimeout { get; init; } = TimeSpan.FromSeconds(10);

	/// <summary>Timeout for CDP HTTP discovery (<c>/json</c> endpoint).</summary>
	public TimeSpan DiscoveryTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
