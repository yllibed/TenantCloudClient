using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization.Metadata;

namespace Yllibed.TenantCloudClient;

public class TcClient : IDisposable, ITcClient
{
	private readonly ITcAuthTokenProvider _tokenProvider;
	private readonly HttpClient _httpClient;

	public TcClient(ITcAuthTokenProvider tokenProvider)
	{
		_tokenProvider = tokenProvider;

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

		var httpHandler = new HttpClientHandler()
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
		var req = new HttpRequestMessage(HttpMethod.Get, uri);
		using var response = await HttpSend(ct, req).ConfigureAwait(false);
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

	private async Task<HttpResponseMessage> HttpSend(CancellationToken ct, HttpRequestMessage request)
	{
		var token = await _tokenProvider.GetToken(ct).ConfigureAwait(false);

		if (string.IsNullOrEmpty(token))
		{
			throw new TcClientException(HttpStatusCode.Unauthorized, "No auth token available");
		}

		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

		if (response.StatusCode != HttpStatusCode.Unauthorized)
		{
			return response;
		}

		// Token was rejected — notify provider and try once more
		response.Dispose();
		await _tokenProvider.OnTokenRejected(ct, token).ConfigureAwait(false);

		var newToken = await _tokenProvider.GetToken(ct).ConfigureAwait(false);

		if (string.IsNullOrEmpty(newToken) || string.Equals(newToken, token, StringComparison.Ordinal))
		{
			throw new TcClientException(HttpStatusCode.Unauthorized, "Auth token rejected and no new token available");
		}

		// HttpRequestMessage cannot be reused after SendAsync, so create a new one
		var retryRequest = new HttpRequestMessage(request.Method, request.RequestUri);
		retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

		return await _httpClient.SendAsync(retryRequest, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
	}

	public void Dispose()
	{
		_httpClient.Dispose();
	}
}
