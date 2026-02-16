using Yllibed.TenantCloudClient.Cdp;

namespace Yllibed.TenantCloudClient.Tests;

public class TestBase
{
	private static readonly bool IsCI =
		!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
		|| !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"));

	private static ITcAuthTokenProvider? s_cachedProvider;
	private static bool s_providerResolved;

	protected ITcAuthTokenProvider TokenProvider { get; private set; } = null!;

	[TestInitialize]
	public async Task EnsureTokenAvailable()
	{
		if (!s_providerResolved)
		{
			s_cachedProvider = await ResolveProvider().ConfigureAwait(false);
			s_providerResolved = true;
		}

		if (s_cachedProvider is null)
		{
			Assert.Inconclusive(
				"No TenantCloud auth token available. " +
				"Set TC_AUTH_TOKEN or sign in via tc-mcp to run integration tests.");
		}

		TokenProvider = s_cachedProvider;
	}

	private static async Task<ITcAuthTokenProvider?> ResolveProvider()
	{
		// 1. Environment variable (CI / manual override)
		var envToken = Environment.GetEnvironmentVariable("TC_AUTH_TOKEN");
		if (!string.IsNullOrEmpty(envToken))
		{
			return new StaticTokenProvider(envToken);
		}

		// 2. CdpTokenProvider: store → browser → interactive login (local only)
		if (SecureTokenStore.IsSupported)
		{
			try
			{
				var provider = new CdpTokenProvider(new CdpTokenProviderOptions
				{
					TokenStore = new SecureTokenStore(),
					AllowInteractiveLogin = !IsCI,
				});

				var token = await provider.GetToken(CancellationToken.None).ConfigureAwait(false);
				if (!string.IsNullOrEmpty(token))
				{
					return provider;
				}
			}
			catch
			{
				// Provider unavailable — fall through
			}
		}

		return null;
	}
}
