namespace Yllibed.TenantCloudClient;

/// <summary>
/// Provides Bearer tokens for TenantCloud API authentication.
/// </summary>
public interface ITcAuthTokenProvider
{
	/// <summary>
	/// Returns a valid Bearer token, or <c>null</c> if no token is available.
	/// </summary>
	Task<string?> GetToken(CancellationToken ct);

	/// <summary>
	/// Called when the server returns 401 Unauthorized.
	/// The provider should invalidate its cached token so that the next
	/// <see cref="GetToken"/> call attempts a refresh.
	/// </summary>
	/// <param name="ct">Cancellation token.</param>
	/// <param name="rejectedToken">
	/// The token that was rejected, to avoid a concurrency bug where two
	/// simultaneous requests both receive 401.
	/// </param>
	Task OnTokenRejected(CancellationToken ct, string rejectedToken);
}
