using Yllibed.TenantCloudClient.Cdp;

namespace Yllibed.TenantCloudClient.Mcp;

internal static class AuthCommands
{
	public static async Task<int> LoginAsync()
	{
		if (!SecureTokenStore.IsSupported)
		{
			await Console.Error.WriteLineAsync("Error: Secure token storage is not supported on this platform.").ConfigureAwait(false);
			return 1;
		}

		var store = new SecureTokenStore();

		// Check if already logged in with a valid token
		var existing = await store.LoadAsync(CancellationToken.None).ConfigureAwait(false);
		if (existing is not null)
		{
			await Console.Out.WriteLineAsync("Already logged in. Use 'tc-mcp logout' first to re-authenticate.").ConfigureAwait(false);
			return 0;
		}

		await Console.Out.WriteLineAsync("Logging in to TenantCloud...").ConfigureAwait(false);

		using var provider = new CdpTokenProvider(new CdpTokenProviderOptions
		{
			TokenStore = store,
			AllowInteractiveLogin = true,
		});

		var token = await provider.GetToken(CancellationToken.None).ConfigureAwait(false);

		if (string.IsNullOrEmpty(token))
		{
			await Console.Error.WriteLineAsync("Login failed. Could not obtain a valid token.").ConfigureAwait(false);
			return 1;
		}

		await Console.Out.WriteLineAsync("Login successful. Tokens stored in secure credential store.").ConfigureAwait(false);
		return 0;
	}

	public static async Task<int> LogoutAsync()
	{
		if (!SecureTokenStore.IsSupported)
		{
			await Console.Error.WriteLineAsync("Error: Secure token storage is not supported on this platform.").ConfigureAwait(false);
			return 1;
		}

		var store = new SecureTokenStore();
		await store.DeleteAsync(CancellationToken.None).ConfigureAwait(false);

		await Console.Out.WriteLineAsync("Logged out. Stored tokens have been removed.").ConfigureAwait(false);
		return 0;
	}
}
