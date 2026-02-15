namespace Yllibed.TenantCloudClient.Tests;

public class TestBase
{
	private static readonly string? s_envToken =
		Environment.GetEnvironmentVariable("TC_AUTH_TOKEN");

	protected ITcAuthTokenProvider TokenProvider { get; private set; } = null!;

	[TestInitialize]
	public void EnsureTokenAvailable()
	{
		var token = s_envToken;

		if (string.IsNullOrEmpty(token))
		{
			Assert.Inconclusive(
				"No TenantCloud auth token available. " +
				"Set TC_AUTH_TOKEN to run integration tests.");
		}

		TokenProvider = new StaticTokenProvider(token);
	}
}
