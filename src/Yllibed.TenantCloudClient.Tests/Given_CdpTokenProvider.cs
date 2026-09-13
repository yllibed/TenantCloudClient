using System.Net;
using AwesomeAssertions;
using Yllibed.TenantCloudClient.Cdp;

namespace Yllibed.TenantCloudClient.Tests;

[TestClass]
public sealed class Given_CdpTokenProvider
{
	[TestMethod]
	public async Task When_BrowserCannotStart_Then_ReturnsNoTokenAndCanRetry()
	{
		using var provider = CreateProvider();

		(await provider.GetToken(CancellationToken.None)).Should().BeNull();
		(await provider.GetToken(CancellationToken.None)).Should().BeNull();
	}

	[TestMethod]
	public async Task When_BrowserCannotStart_Then_ClientReportsUnauthorized()
	{
		using var provider = CreateProvider();
		using var client = new TcClient(provider);

		Func<Task> act = () => client.GetUserInfo(CancellationToken.None);

		var failure = await act.Should().ThrowAsync<TcClientException>();
		failure.Which.HttpStatus.Should().Be(HttpStatusCode.Unauthorized);
	}

	[TestMethod]
	public async Task When_CancelledDuringTokenLookup_Then_DoesNotAttemptInteractiveLogin()
	{
		using var cancellation = new CancellationTokenSource();
		using var provider = CreateProvider(new CancellingTokenStore(cancellation));

		Func<Task> act = () => provider.GetToken(cancellation.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	private static CdpTokenProvider CreateProvider(ITcTokenStore? store = null) => new(new()
	{
		AllowInteractiveLogin = true,
		BrowserExecutablePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing-browser"),
		DebugPort = 0,
		TokenStore = store,
	});

	private sealed class CancellingTokenStore(CancellationTokenSource cancellation) : ITcTokenStore
	{
		public async Task<TcTokenSet?> LoadAsync(CancellationToken ct)
		{
			await cancellation.CancelAsync().ConfigureAwait(false);
			return null;
		}

		public Task SaveAsync(TcTokenSet tokens, CancellationToken ct) => throw new NotSupportedException();
		public Task DeleteAsync(CancellationToken ct) => throw new NotSupportedException();
	}
}
