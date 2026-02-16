namespace Yllibed.TenantCloudClient.Internal;

/// <summary>
/// Platform-specific backend for loading and saving raw credential bytes.
/// </summary>
internal interface ISecureStorageBackend
{
	Task<byte[]?> LoadAsync(string serviceName, string accountKey, CancellationToken ct);
	Task SaveAsync(string serviceName, string accountKey, byte[] data, CancellationToken ct);
	Task DeleteAsync(string serviceName, string accountKey, CancellationToken ct);
}
