using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization.Metadata;
using Yllibed.TenantCloudClient.Internal;

namespace Yllibed.TenantCloudClient;

public class TcClient : IDisposable, ITcClient
{
	private readonly ITcAuthTokenProvider _tokenProvider;
	private readonly HttpClient _httpClient;
	private readonly TcRateLimitOptions _rateLimitOptions;
	private readonly TimeProvider _timeProvider;
	private readonly Func<double> _jitter;
	private readonly TcRequestScheduler _scheduler;

	public TcClient(ITcAuthTokenProvider tokenProvider)
		: this(tokenProvider, new TcRateLimitOptions())
	{
	}

	/// <summary>Creates a client with per-instance pacing and HTTP 429 retry settings.</summary>
	public TcClient(ITcAuthTokenProvider tokenProvider, TcRateLimitOptions options)
		: this(tokenProvider, options, null, TimeProvider.System, Random.Shared.NextDouble)
	{
	}

	internal TcClient(ITcAuthTokenProvider tokenProvider, TcRateLimitOptions options,
		HttpMessageHandler? handler, TimeProvider timeProvider, Func<double> jitter)
	{
		ArgumentNullException.ThrowIfNull(tokenProvider);
		ArgumentNullException.ThrowIfNull(options);
		TcRateLimitOptions.Validate(options);
		_tokenProvider = tokenProvider;
		_rateLimitOptions = options;
		_timeProvider = timeProvider;
		_jitter = jitter;
		_scheduler = new(options, timeProvider);

		Contacts = new PaginatedSource<TcContact>(
			(ct, page, extra) => GetJsonApiPage(ct, "contacts", page, extra,
				TcJsonSerializerContext.Default.TcJsonApiResponseTcContact), "");

		Properties = new PaginatedSource<TcProperty>(
			(ct, page, extra) => GetJsonApiPage(ct, "properties", page, extra,
				TcJsonSerializerContext.Default.TcJsonApiResponseTcProperty), "");

		Units = new PaginatedSource<TcUnit>(
			(ct, page, extra) => GetJsonApiPage(ct, "units", page, extra,
				TcJsonSerializerContext.Default.TcJsonApiResponseTcUnit), "");

		Transactions = new PaginatedSource<TcTransaction>(
			(ct, page, extra) => GetJsonApiPage(ct, "transactions", page, extra,
				TcJsonSerializerContext.Default.TcJsonApiResponseTcTransaction), "");

		Leases = new PaginatedSource<TcLease>(
			(ct, page, extra) => GetJsonApiPage(ct, "leases", page, extra,
				TcJsonSerializerContext.Default.TcJsonApiResponseTcLease), "");

		var httpHandler = handler ?? new HttpClientHandler()
		{
			UseCookies = false,
			UseDefaultCredentials = false,
			AllowAutoRedirect = true,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
		};

		_httpClient = new HttpClient(httpHandler, true)
		{
			BaseAddress = new Uri("https://api.tenantcloud.com/"),
		};

		_httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Yllibed.TenantCloudClient", "0.1"));
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/json"));
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/*"));
	}

	public async Task<TcUserInfo?> GetUserInfo(CancellationToken ct)
	{
		var result = await HttpGet(ct, "auth/user", TcJsonSerializerContext.Default.TcUserInfoResponse).ConfigureAwait(false);
		return result?.User;
	}

	public IPaginatedSource<TcContact> Contacts { get; }

	public IPaginatedSource<TcProperty> Properties { get; }

	public IPaginatedSource<TcUnit> Units { get; }

	public IPaginatedSource<TcTransaction> Transactions { get; }

	public IPaginatedSource<TcLease> Leases { get; }

	private async Task<(ReadOnlyMemory<T>, long, long)> GetJsonApiPage<T>(
		CancellationToken ct, string endpoint, long pageNo, string extraUrl,
		JsonTypeInfo<TcJsonApiResponse<T>> typeInfo)
		where T : class, IHasId
	{
		var url = endpoint + "?page=" + pageNo.ToString(CultureInfo.InvariantCulture) + extraUrl;
		var response = await HttpGet(ct, url, typeInfo).ConfigureAwait(false);

		var entries = response.Data?
			.Select(item =>
			{
				var attr = item.Attributes;
				if (attr is not null)
				{
					attr.Id = item.Id;
				}

				return attr!;
			})
			.Where(a => a is not null)
			.ToArray() ?? Array.Empty<T>();

		return (entries.AsMemory(), pageNo, response.Meta?.Pagination?.Total ?? 0);
	}

	private async Task<T> HttpGet<T>(CancellationToken ct, string uri, JsonTypeInfo<T> typeInfo)
	{
		var budget = _scheduler.CreateBudget();
		await _scheduler.EnterAsync(budget, ct).ConfigureAwait(false);
		try
		{
			return await ReadResponse(ct, uri, typeInfo, budget).ConfigureAwait(false);
		}
		finally
		{
			_scheduler.Exit();
		}
	}

	private async Task<T> ReadResponse<T>(CancellationToken ct, string uri, JsonTypeInfo<T> typeInfo, TcRequestScheduler.WaitBudget budget)
	{
		using var response = await HttpSend(ct, uri, budget).ConfigureAwait(false);
		var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		await using var _ = stream.ConfigureAwait(false);

		if (response.IsSuccessStatusCode)
		{
			var payload = await JsonSerializer.DeserializeAsync(stream, typeInfo, ct).ConfigureAwait(false);
			return payload ?? throw new TcClientException(response.StatusCode, "Null response payload");
		}
		else
		{
			var errorPayload = await JsonSerializer.DeserializeAsync(stream, TcJsonSerializerContext.Default.TcErrorResponse, ct).ConfigureAwait(false);
			throw new TcClientException(response.StatusCode, errorPayload?.Message ?? "Http error");
		}
	}

	private async Task<HttpResponseMessage> HttpSend(CancellationToken ct, string uri, TcRequestScheduler.WaitBudget budget)
	{
		var token = await _tokenProvider.GetToken(ct).ConfigureAwait(false);

		if (string.IsNullOrEmpty(token))
		{
			throw new TcClientException(HttpStatusCode.Unauthorized, "No auth token available");
		}

		var authenticationRetried = false;
		var retries = 0;
		while (true)
		{
			await _scheduler.WaitForTurnAsync(budget, ct).ConfigureAwait(false);
			using var request = new HttpRequestMessage(HttpMethod.Get, uri);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
			_scheduler.MarkStart();
			var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
			if (response.StatusCode == HttpStatusCode.TooManyRequests)
			{
				TcRateLimitException error;
				using (response)
				{
					error = new TcRateLimitException(response, _timeProvider);
				}
				if (!_rateLimitOptions.Enabled)
				{
					throw error;
				}
				var delay = error.RetryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, Math.Min(retries, 30)) + (_jitter() * 0.25));
				_scheduler.Pause(delay);
				budget.RateLimitFailure = error;
				if (retries >= _rateLimitOptions.MaxRetries)
				{
					throw error;
				}
				retries++;
				continue;
			}
			if (response.StatusCode != HttpStatusCode.Unauthorized || authenticationRetried)
			{
				return response;
			}
			response.Dispose();
			authenticationRetried = true;
			await _tokenProvider.OnTokenRejected(ct, token).ConfigureAwait(false);
			var newToken = await _tokenProvider.GetToken(ct).ConfigureAwait(false);
			if (string.IsNullOrEmpty(newToken) || string.Equals(newToken, token, StringComparison.Ordinal))
			{
				throw new TcClientException(HttpStatusCode.Unauthorized, "Auth token rejected and no new token available");
			}
			token = newToken;
		}
	}

	public void Dispose()
	{
		_httpClient.Dispose();
		_scheduler.Dispose();
	}
}
