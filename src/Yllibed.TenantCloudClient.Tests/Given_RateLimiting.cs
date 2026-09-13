using System.Net;
using System.Net.Http;
using System.Globalization;
using System.Threading.Channels;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Repl.Testing;
using Yllibed.TenantCloudClient.Mcp;

namespace Yllibed.TenantCloudClient.Tests;

[TestClass]
public sealed class Given_RateLimiting
{
	[TestMethod]
	public async Task When_ServerRequestsPause_Then_RetriesAfterDelay()
	{
		var time = new FakeTimeProvider();
		using var handler = new Replies(Limited("5"), Ok());
		using var client = Create(handler, time);
		var pending = client.GetUserInfo(CancellationToken.None);
		handler.Calls.Should().Be(1);
		pending.IsCompleted.Should().BeFalse();
		time.Advance(TimeSpan.FromSeconds(5));
		(await pending.WaitAsync(TimeSpan.FromSeconds(5)))!.Id.Should().Be(1);
		handler.Calls.Should().Be(2);
	}

	[TestMethod]
	public async Task When_PauseExceedsBudget_Then_PreservesSafeMetadata()
	{
		var response = Limited("60");
		response.Headers.TryAddWithoutValidation("X-Ratelimit-Limit", "60");
		response.Headers.TryAddWithoutValidation("X-Ratelimit-Remaining", "0");
		using var handler = new Replies(response);
		using var client = Create(handler, new FakeTimeProvider());
		Func<Task> act = () => client.GetUserInfo(CancellationToken.None);
		var error = (await act.Should().ThrowAsync<TcRateLimitException>()).Which;
		error.HttpStatus.Should().Be(HttpStatusCode.TooManyRequests);
		error.RetryAfter.Should().Be(TimeSpan.FromSeconds(60));
		error.Limit.Should().Be(60);
		error.Remaining.Should().Be(0);
		error.Message.Should().NotContain("private-response");
		handler.Calls.Should().Be(1);
	}

	[TestMethod]
	public async Task When_DefaultCadence_Then_SpacesCollections()
	{
		var time = new FakeTimeProvider();
		using var handler = new Replies(Ok(), new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("{\"data\":[],\"meta\":{\"pagination\":{\"total\":0}}}"),
		});
		using var client = Create(handler, time);
		await client.GetUserInfo(CancellationToken.None);
		var pending = client.Properties.GetPage(CancellationToken.None);
		handler.Calls.Should().Be(1);
		time.Advance(TimeSpan.FromSeconds(1));
		await pending.WaitAsync(TimeSpan.FromSeconds(5));
		handler.Calls.Should().Be(2);
	}

	private static TcClient Create(HttpMessageHandler handler, TimeProvider time, TcRateLimitOptions? options = null) =>
		new(new StaticTokenProvider("test-token"), options ?? new(), handler, time, () => 0);

	[TestMethod]
	[DataRow(null)]
	[DataRow("not-a-delay")]
	[DataRow("-1")]
	public async Task When_RetryHeaderIsUnavailable_Then_BackoffIncludesJitter(string? header)
	{
		var time = new ObservedTime();
		using var handler = new Replies(Limited(header), Limited(header), Limited(header), Ok());
		using var client = new TcClient(new StaticTokenProvider("test-token"), new() { MinRequestInterval = TimeSpan.Zero }, handler, time, () => 0.5);
		var pending = client.GetUserInfo(CancellationToken.None);
		foreach (var seconds in new[] { 1.125, 2.125, 4.125 })
		{
			var delay = await time.NextDelay();
			delay.Should().Be(TimeSpan.FromSeconds(seconds));
			time.Advance(delay);
		}
		await pending.WaitAsync(TimeSpan.FromSeconds(5));
		handler.Calls.Should().Be(4);
	}

	[TestMethod]
	[DataRow(5, true)]
	[DataRow(-5, true)]
	[DataRow(5, false)]
	public async Task When_RetryDate_Then_UsesServerClockWhenAvailable(int seconds, bool serverDate)
	{
		var time = new FakeTimeProvider();
		var baseline = serverDate ? time.GetUtcNow().AddHours(2) : time.GetUtcNow();
		var response = Limited(baseline.AddSeconds(seconds).ToString("r", CultureInfo.InvariantCulture));
		if (serverDate) { response.Headers.Date = baseline; }
		using var handler = new Replies(response, Ok());
		using var client = Create(handler, time, new() { MinRequestInterval = TimeSpan.Zero });
		var pending = client.GetUserInfo(CancellationToken.None);
		if (seconds > 0)
		{
			handler.Calls.Should().Be(1);
			time.Advance(TimeSpan.FromSeconds(seconds));
		}
		await pending.WaitAsync(TimeSpan.FromSeconds(5));
		handler.Calls.Should().Be(2);
	}

	[TestMethod]
	public async Task When_AlwaysLimited_Then_StopsAtRetryCount()
	{
		using var handler = new Replies(Limited("0"), Limited("0"), Limited("0"), Limited("0"));
		using var client = Create(handler, new FakeTimeProvider(), new() { MinRequestInterval = TimeSpan.Zero });
		Func<Task> act = () => client.GetUserInfo(CancellationToken.None);
		await act.Should().ThrowAsync<TcRateLimitException>();
		handler.Calls.Should().Be(4);
	}

	[TestMethod]
	public async Task When_WaitsAccumulate_Then_DoesNotShortenServerDelay()
	{
		var time = new FakeTimeProvider();
		using var handler = new Replies(Limited("20"), Limited("20"));
		using var client = Create(handler, time);
		var pending = client.GetUserInfo(CancellationToken.None);
		time.Advance(TimeSpan.FromSeconds(20));
		Func<Task> act = () => pending.WaitAsync(TimeSpan.FromSeconds(5));
		await act.Should().ThrowAsync<TcRateLimitException>();
		handler.Calls.Should().Be(2);
	}

	[TestMethod]
	public async Task When_AuthenticationRetries_Then_DoesNotResetWaitBudget()
	{
		var time = new FakeTimeProvider();
		var auth = new RefreshableToken();
		using var handler = new Replies(Limited("20"), new(HttpStatusCode.Unauthorized), Limited("20"));
		using var client = new TcClient(auth, new() { MinRequestInterval = TimeSpan.Zero }, handler, time, () => 0);
		var pending = client.GetUserInfo(CancellationToken.None);
		time.Advance(TimeSpan.FromSeconds(20));
		Func<Task> act = () => pending.WaitAsync(TimeSpan.FromSeconds(5));
		await act.Should().ThrowAsync<TcRateLimitException>();
		handler.Calls.Should().Be(3);
		auth.Rejections.Should().Be(1);
	}

	[TestMethod]
	public async Task When_CancelledDuringPause_Then_NextReadStillWaits()
	{
		var time = new FakeTimeProvider();
		using var handler = new Replies(Limited("10"), Ok());
		using var client = Create(handler, time);
		using var cancellation = new CancellationTokenSource();
		var pending = client.GetUserInfo(cancellation.Token);
		await cancellation.CancelAsync();
		Func<Task> cancelled = () => pending.WaitAsync(TimeSpan.FromSeconds(5));
		await cancelled.Should().ThrowAsync<OperationCanceledException>();
		var next = client.GetUserInfo(CancellationToken.None);
		handler.Calls.Should().Be(1);
		time.Advance(TimeSpan.FromSeconds(10));
		await next.WaitAsync(TimeSpan.FromSeconds(5));
		handler.Calls.Should().Be(2);
	}

	[TestMethod]
	public async Task When_RetryCountIsZero_Then_PauseIsStillShared()
	{
		var time = new FakeTimeProvider();
		using var handler = new Replies(Limited("5"), Ok());
		using var client = Create(handler, time, new() { MaxRetries = 0 });
		Func<Task> first = () => client.GetUserInfo(CancellationToken.None);
		await first.Should().ThrowAsync<TcRateLimitException>();
		var second = client.GetUserInfo(CancellationToken.None);
		handler.Calls.Should().Be(1);
		time.Advance(TimeSpan.FromSeconds(5));
		await second.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[TestMethod]
	public async Task When_Queued_Then_TimeoutDoesNotInventHttpStatus()
	{
		var time = new FakeTimeProvider();
		using var handler = new BlockedReply();
		using var client = Create(handler, time);
		var first = client.GetUserInfo(CancellationToken.None);
		var queued = client.GetUserInfo(CancellationToken.None);
		time.Advance(TimeSpan.FromSeconds(30));
		Func<Task> act = () => queued.WaitAsync(TimeSpan.FromSeconds(5));
		await act.Should().ThrowAsync<TimeoutException>();
		handler.Calls.Should().Be(1);
		handler.Release();
		await first.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[TestMethod]
	public async Task When_QueuedAndCancelled_Then_DoesNotSendOrLeakPermit()
	{
		var time = new FakeTimeProvider();
		using var handler = new BlockedReply();
		using var client = Create(handler, time, new() { MinRequestInterval = TimeSpan.Zero });
		var first = client.GetUserInfo(CancellationToken.None);
		using var cancellation = new CancellationTokenSource();
		var queued = client.GetUserInfo(cancellation.Token);
		await cancellation.CancelAsync();
		Func<Task> act = () => queued.WaitAsync(TimeSpan.FromSeconds(5));
		await act.Should().ThrowAsync<OperationCanceledException>();
		handler.Calls.Should().Be(1);
		handler.Release();
		await first.WaitAsync(TimeSpan.FromSeconds(5));
		await client.GetUserInfo(CancellationToken.None);
		handler.Calls.Should().Be(2);
	}

	[TestMethod]
	public async Task When_NetworkIsSlow_Then_DoesNotConsumeWaitBudget()
	{
		var time = new FakeTimeProvider();
		using var handler = new BlockedReply();
		using var client = Create(handler, time);
		var first = client.GetUserInfo(CancellationToken.None);
		time.Advance(TimeSpan.FromSeconds(60));
		handler.Release();
		await first.WaitAsync(TimeSpan.FromSeconds(5));
		await client.GetUserInfo(CancellationToken.None);
		handler.Calls.Should().Be(2);
	}

	[TestMethod]
	public async Task When_Disabled_Then_DoesNotRetryOrSerialize()
	{
		using var handler = new BlockedReply();
		using var client = Create(handler, new FakeTimeProvider(), new() { Enabled = false });
		var first = client.GetUserInfo(CancellationToken.None);
		var second = client.GetUserInfo(CancellationToken.None);
		handler.Calls.Should().Be(2);
		handler.Release();
		await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
		using var limited = new Replies(Limited("5"));
		using var other = Create(limited, new FakeTimeProvider(), new() { Enabled = false });
		Func<Task> act = () => other.GetUserInfo(CancellationToken.None);
		await act.Should().ThrowAsync<TcRateLimitException>();
		limited.Calls.Should().Be(1);
	}

	[TestMethod]
	public async Task When_SeparateClients_Then_DoNotShareCooldown()
	{
		var time = new FakeTimeProvider();
		using var firstHandler = new Replies(Limited("60"));
		using var secondHandler = new Replies(Ok());
		using var first = Create(firstHandler, time);
		using var second = Create(secondHandler, time);
		Func<Task> act = () => first.GetUserInfo(CancellationToken.None);
		await act.Should().ThrowAsync<TcRateLimitException>();
		await second.GetUserInfo(CancellationToken.None);
		secondHandler.Calls.Should().Be(1);
	}

	[TestMethod]
	public async Task When_UnauthorizedAndLimited_Then_AuthRetriesOnlyOnce()
	{
		var auth = new RefreshableToken();
		using var handler = new Replies(new(HttpStatusCode.Unauthorized), Limited("0"), new(HttpStatusCode.Unauthorized)
		{
			Content = new StringContent("{\"message\":\"Unauthorized\"}"),
		});
		using var client = new TcClient(auth, new() { MinRequestInterval = TimeSpan.Zero }, handler, new FakeTimeProvider(), () => 0);
		Func<Task> act = () => client.GetUserInfo(CancellationToken.None);
		(await act.Should().ThrowAsync<TcClientException>()).Which.HttpStatus.Should().Be(HttpStatusCode.Unauthorized);
		auth.Rejections.Should().Be(1);
		handler.Calls.Should().Be(3);
	}

	[TestMethod]
	[DataRow(400)]
	[DataRow(403)]
	[DataRow(500)]
	[DataRow(503)]
	public async Task When_OtherErrors_Then_DoesNotRetry(int status)
	{
		using var handler = new Replies(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("{\"message\":\"error\"}") });
		using var client = Create(handler, new FakeTimeProvider());
		Func<Task> act = () => client.GetUserInfo(CancellationToken.None);
		(await act.Should().ThrowAsync<TcClientException>()).Which.HttpStatus.Should().Be((HttpStatusCode)status);
		handler.Calls.Should().Be(1);
	}

	[TestMethod]
	public async Task When_HeadersAreInvalid_Then_QuotaIsUnknown()
	{
		var response = Limited("invalid");
		response.Headers.TryAddWithoutValidation("X-Ratelimit-Limit", "-1");
		response.Headers.TryAddWithoutValidation("X-Ratelimit-Remaining", "9999999999999999999999");
		using var handler = new Replies(response);
		using var client = Create(handler, new FakeTimeProvider(), new() { MaxRetries = 0 });
		Func<Task> act = () => client.GetUserInfo(CancellationToken.None);
		var error = (await act.Should().ThrowAsync<TcRateLimitException>()).Which;
		error.RetryAfter.Should().BeNull();
		error.Limit.Should().BeNull();
		error.Remaining.Should().BeNull();
	}

	[TestMethod]
	public void When_OptionsAreInvalid_Then_FailsBeforeUsingTransport()
	{
		foreach (var options in new[]
		{
			new TcRateLimitOptions { MaxRetries = -1 },
			new TcRateLimitOptions { MinRequestInterval = TimeSpan.FromTicks(-1) },
			new TcRateLimitOptions { MaxWait = TimeSpan.FromTicks(-1) },
			new TcRateLimitOptions { MaxWait = TimeSpan.MaxValue },
		})
		{
			Action act = () => new TcClient(new StaticTokenProvider("test-token"), options);
			act.Should().Throw<ArgumentOutOfRangeException>();
		}
	}

	[TestMethod]
	public void When_UsingDependencyInjection_Then_BothRegistrationsWork()
	{
		foreach (var configured in new[] { false, true })
		{
			var services = new ServiceCollection();
			services.AddSingleton<ITcAuthTokenProvider>(new StaticTokenProvider("test-token"));
			if (configured) { services.AddTenantCloudClient(new TcRateLimitOptions { Enabled = false }); }
			else { services.AddTenantCloudClient(); }
			using var provider = services.BuildServiceProvider();
			provider.GetRequiredService<ITcClient>().Should().BeSameAs(provider.GetRequiredService<ITcClient>());
		}
		using var legacy = new TcClient(new StaticTokenProvider("test-token"));
	}

	[TestMethod]
	public async Task When_ReplUsesRealClient_Then_RateLimitedReadRecovers()
	{
		using var handler = new Replies(Limited("0"), Ok());
		using var client = Create(handler, new FakeTimeProvider(), new() { MinRequestInterval = TimeSpan.Zero });
		await using var host = ReplTestHost.Create(() => TenantCloudApp.Create(s => s.AddSingleton<ITcClient>(client)));
		await using var session = await host.OpenSessionAsync();
		var result = await session.RunCommandAsync("get user info --json --no-logo");
		result.ExitCode.Should().Be(0, result.OutputText);
		handler.Calls.Should().Be(2);
	}

	[TestMethod]
	public async Task When_Retrying_Then_DisposesResponsesAndCreatesFreshRequestsWithoutRefreshingToken()
	{
		var limited = new TrackedResponse(HttpStatusCode.TooManyRequests);
		limited.Headers.TryAddWithoutValidation("Retry-After", "0");
		var success = new TrackedResponse(HttpStatusCode.OK) { Content = new StringContent("{\"user\":{\"id\":1}}") };
		var auth = new RefreshableToken();
		using var handler = new Replies(limited, success);
		using var client = new TcClient(auth, new() { MinRequestInterval = TimeSpan.Zero }, handler, new FakeTimeProvider(), () => 0);
		await client.GetUserInfo(CancellationToken.None);
		limited.Disposed.Should().BeTrue();
		success.Disposed.Should().BeTrue();
		handler.Requests[0].Should().NotBeSameAs(handler.Requests[1]);
		foreach (var request in handler.Requests)
		{
			Action mutate = () => request.Method = HttpMethod.Post;
			mutate.Should().Throw<ObjectDisposedException>();
		}
		auth.Rejections.Should().Be(0);
	}

	private sealed class TrackedResponse(HttpStatusCode status) : HttpResponseMessage(status)
	{
		public bool Disposed { get; private set; }
		protected override void Dispose(bool disposing)
		{
			Disposed = true;
			base.Dispose(disposing);
		}
	}

	private sealed class ObservedTime : TimeProvider
	{
		private readonly FakeTimeProvider _time = new();
		private readonly Channel<TimeSpan> _delays = Channel.CreateUnbounded<TimeSpan>();
		public override DateTimeOffset GetUtcNow() => _time.GetUtcNow();
		public override long GetTimestamp() => _time.GetTimestamp();
		public override long TimestampFrequency => _time.TimestampFrequency;
		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
		{
			var timer = _time.CreateTimer(callback, state, dueTime, period);
			_delays.Writer.TryWrite(dueTime);
			return timer;
		}
		public void Advance(TimeSpan delay) => _time.Advance(delay);
		public Task<TimeSpan> NextDelay() => _delays.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
	}

	private sealed class BlockedReply : HttpMessageHandler
	{
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Calls { get; private set; }
		public void Release() => _release.TrySetResult();
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Calls++;
			await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
			return Ok();
		}
	}

	private sealed class RefreshableToken : ITcAuthTokenProvider
	{
		public int Rejections { get; private set; }
		public Task<string?> GetToken(CancellationToken ct) => Task.FromResult<string?>(Rejections == 0 ? "first-token" : "second-token");
		public Task OnTokenRejected(CancellationToken ct, string rejectedToken)
		{
			Rejections++;
			return Task.CompletedTask;
		}
	}

	private static HttpResponseMessage Ok() => new(HttpStatusCode.OK)
	{
		Content = new StringContent("{\"user\":{\"id\":1}}"),
	};

	private static HttpResponseMessage Limited(string? retryAfter = null)
	{
		var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
		{
			Content = new StringContent("<html>private-response</html>"),
		};
		if (retryAfter is not null)
		{
			response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
		}
		return response;
	}

	private sealed class Replies(params HttpResponseMessage[] responses) : HttpMessageHandler
	{
		public int Calls { get; private set; }
		public List<HttpRequestMessage> Requests { get; } = [];
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Requests.Add(request);
			return Task.FromResult(responses[Calls++]);
		}
	}
}
