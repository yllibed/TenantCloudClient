using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient;

public class TcClient : IDisposable, ITcClient
{
	private readonly ITcAuthTokenProvider _tokenProvider;
	private readonly HttpClient _httpClient;

	public TcClient(ITcAuthTokenProvider tokenProvider)
	{
		_tokenProvider = tokenProvider;

		Tenants = new PaginatedSource<TcTenantDetails>(GetTenantPage, "");

		Properties = new PaginatedSource<TcProperty>(GetPropertyPage, "");

		Units = new PaginatedSource<TcUnit>(GetUnitsPage, "");

		Transactions = new PaginatedSource<TcTransaction>(GetTransactionsPage, "");

		var httpHandler = new HttpClientHandler()
		{
			UseCookies = false,
			UseDefaultCredentials = false,
			AllowAutoRedirect = true,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
		};

		_httpClient = new HttpClient(httpHandler, true)
		{
			BaseAddress = new Uri("https://home.tenantcloud.com/"),
		};

		_httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Yllibed.TenantCloudClient", "0.1"));
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/json"));
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
		_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/*"));
	}

	public async Task<TcUserInfo?> GetUserInfo(CancellationToken ct)
	{
		var result = await HttpGet(ct, "v1/auth/user", TcJsonSerializerContext.Default.TcUserInfoResponse).ConfigureAwait(false);
		return result?.User;
	}

	public IPaginatedSource<TcTenantDetails> Tenants { get; }

	private async Task<(ReadOnlyMemory<TcTenantDetails>, long, long)> GetTenantPage(CancellationToken ct, long pageNo, string extraUrl)
	{
		var response = await HttpGet(ct, "v1/landlord/tenants?page=" + pageNo.ToString(System.Globalization.CultureInfo.InvariantCulture) + extraUrl, TcJsonSerializerContext.Default.TcListResponseTcTenantDetails).ConfigureAwait(false);
		var memory = new Memory<TcTenantDetails>(response.Entries);
		return (memory, pageNo, response?.Pagination?.Total ?? 0);
	}

	public IPaginatedSource<TcProperty> Properties { get; }

	private async Task<(ReadOnlyMemory<TcProperty>, long, long)> GetPropertyPage(CancellationToken ct, long pageNo, string extraUrl)
	{
		var response = await HttpGet(ct, "v2/property?fields[property]=name,property_status,address1,cityAddress&page=" + pageNo.ToString(System.Globalization.CultureInfo.InvariantCulture) + extraUrl, TcJsonSerializerContext.Default.TcPagingListResponseTcProperty).ConfigureAwait(false);
		var memory = new Memory<TcProperty>(response.Entries);
		return (memory, pageNo, response?.Meta?.Pagination?.Total ?? 0);
	}

	public IPaginatedSource<TcUnit> Units { get; }

	private async Task<(ReadOnlyMemory<TcUnit>, long, long)> GetUnitsPage(CancellationToken ct, long pageNo, string extraUrl)
	{
		var response = await HttpGet(ct, "v1/landlord/units?page=" + pageNo.ToString(System.Globalization.CultureInfo.InvariantCulture) + extraUrl, TcJsonSerializerContext.Default.TcListResponseTcUnit).ConfigureAwait(false);
		var memory = new Memory<TcUnit>(response.Entries);
		return (memory, pageNo, response?.Pagination?.Total ?? 0);
	}

	public IPaginatedSource<TcTransaction> Transactions { get; }

	private async Task<(ReadOnlyMemory<TcTransaction>, long, long)> GetTransactionsPage(CancellationToken ct, long pageNo, string extraUrl)
	{
		var response = await HttpGet(ct, "v1/landlord/transactions?page=" + pageNo.ToString(System.Globalization.CultureInfo.InvariantCulture) + extraUrl, TcJsonSerializerContext.Default.TcListResponseTcTransaction).ConfigureAwait(false);
		var memory = new Memory<TcTransaction>(response.Entries);
		return (memory, pageNo, response?.Pagination?.Total ?? 0);
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
