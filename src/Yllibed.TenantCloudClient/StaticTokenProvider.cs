namespace Yllibed.TenantCloudClient;

/// <summary>
/// A token provider that returns a fixed token. Once rejected, returns <c>null</c>.
/// </summary>
public class StaticTokenProvider : ITcAuthTokenProvider
{
	private string? _token;

	public StaticTokenProvider(string token)
	{
		_token = token ?? throw new ArgumentNullException(nameof(token));
	}

	public Task<string?> GetToken(CancellationToken ct) => Task.FromResult(_token);

	public Task OnTokenRejected(CancellationToken ct, string rejectedToken)
	{
		// Only invalidate if the rejected token matches the current one
		if (string.Equals(_token, rejectedToken, StringComparison.Ordinal))
		{
			_token = null;
		}

		return Task.CompletedTask;
	}
}
