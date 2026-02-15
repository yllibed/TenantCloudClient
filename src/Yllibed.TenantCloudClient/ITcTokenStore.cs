namespace Yllibed.TenantCloudClient;

/// <summary>
/// Persists TenantCloud auth tokens across sessions.
/// </summary>
public interface ITcTokenStore
{
	Task<TcTokenSet?> LoadAsync(CancellationToken ct);
	Task SaveAsync(TcTokenSet tokens, CancellationToken ct);
}
