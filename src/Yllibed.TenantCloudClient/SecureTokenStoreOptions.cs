namespace Yllibed.TenantCloudClient;

/// <summary>
/// Options for <see cref="SecureTokenStore"/>.
/// </summary>
public sealed class SecureTokenStoreOptions
{
	/// <summary>Service name used as the credential identifier in the OS credential store.</summary>
	public string ServiceName { get; init; } = "Yllibed.TenantCloudClient";

	/// <summary>Account key for distinguishing multiple credential entries.</summary>
	public string AccountKey { get; init; } = "default";
}
