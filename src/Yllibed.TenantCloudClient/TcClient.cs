using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient
{
	public class TcClient : IDisposable, ITcClient
	{
		private readonly ITcContext _context;
		private readonly HttpClient _httpClient;

		private static readonly Encoding _encoding = new UTF8Encoding(false);

		public TcClient(ITcContext context)
		{
			_context = context;

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
				BaseAddress = new Uri("https://home.tenantcloud.com/")
			};

			_httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Yllibed.TenantCloudClient", "0.1"));
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/json"));
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/*"));
		}

		public async Task<TcUserInfo?> GetUserInfo(CancellationToken ct)
		{
			var result = await HttpGet<TcUserInfoResponse>(ct, "v1/auth/user");
			return result?.User;
		}


		public IPaginatedSource<TcTenantDetails> Tenants { get; }

		private async Task<(ReadOnlyMemory<TcTenantDetails>, long, long)> GetTenantPage(CancellationToken ct, long pageNo, string extraUrl)
		{
			var response = await HttpGet<TcListResponse<TcTenantDetails>>(ct, "v1/landlord/tenants?page=" + pageNo + extraUrl);
			var memory = new Memory<TcTenantDetails>(response.Entries);
			return (memory, pageNo, response?.Pagination?.Total ?? 0);
		}

		public IPaginatedSource<TcProperty> Properties { get; }

		private async Task<(ReadOnlyMemory<TcProperty>, long, long)> GetPropertyPage(CancellationToken ct, long pageNo, string extraUrl)
		{
			var response = await HttpGet<TcPagingListResponse<TcProperty>>(ct, "v2/property?fields[property]=name,property_status,address1,cityAddress&page=" + pageNo + extraUrl);
			var memory = new Memory<TcProperty>(response.Entries);
			return (memory, pageNo, response?.Meta?.Pagination?.Total ?? 0);
		}

		public IPaginatedSource<TcUnit> Units { get; }

		private async Task<(ReadOnlyMemory<TcUnit>, long, long)> GetUnitsPage(CancellationToken ct, long pageNo, string extraUrl)
		{
			var response = await HttpGet<TcListResponse<TcUnit>>(ct, "v1/landlord/units?page=" + pageNo + extraUrl);
			var memory = new Memory<TcUnit>(response.Entries);
			return (memory, pageNo, response?.Pagination?.Total ?? 0);
		}

		public IPaginatedSource<TcTransaction> Transactions { get; }

		private async Task<(ReadOnlyMemory<TcTransaction>, long, long)> GetTransactionsPage(CancellationToken ct, long pageNo, string extraUrl)
		{
			var response = await HttpGet<TcListResponse<TcTransaction>>(ct, "v1/landlord/transactions?page=" + pageNo + extraUrl);
			var memory = new Memory<TcTransaction>(response.Entries);
			return (memory, pageNo, response?.Pagination?.Total ?? 0);
		}

		private static readonly JsonSerializerOptions _jsonOptions =
			new JsonSerializerOptions
			{
				AllowTrailingCommas = true,
				PropertyNameCaseInsensitive = true
			};

		private async Task<T> HttpGet<T>(CancellationToken ct, string uri)
		{
			var req = new HttpRequestMessage(HttpMethod.Get, uri);
			using var response = await HttpSend(ct, req);
			await using var stream = await response.Content.ReadAsStreamAsync();

			if (response.IsSuccessStatusCode)
			{
				var payload = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, ct);
				return payload;
			}
			else
			{
				var errorPayload = await JsonSerializer.DeserializeAsync<TcErrorResponse>(stream, _jsonOptions, ct);
				throw new TcClientException(response.StatusCode, errorPayload?.Message ?? "Http error");

			}
		}

		private async Task<HttpResponseMessage> HttpSend(CancellationToken ct, HttpRequestMessage request)
		{
			var token = await _context.GetAuthToken(ct);

			if (!string.IsNullOrEmpty(token))
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
				var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

				if (response.StatusCode != HttpStatusCode.Unauthorized)
				{
					return response;
				}

				request.Headers.Authorization = null;
			}

			var loginRequest = new TcLoginRequest(await _context.GetCredentials(ct));
			var loginRequestMsg = new HttpRequestMessage(HttpMethod.Post, "v1/auth/login")
			{
				Content = GetJsonContent(loginRequest)
			};

			var loginResponse = await _httpClient.SendAsync(loginRequestMsg, HttpCompletionOption.ResponseHeadersRead, ct);

			if (!loginResponse.IsSuccessStatusCode)
			{
				throw new TcClientException(loginResponse.StatusCode, "Unable to login");
			}

			await using var loginResponseStream = await loginResponse.Content.ReadAsStreamAsync();
			var loginResponsePayload = await JsonSerializer.DeserializeAsync<TcLoginResponse?>(loginResponseStream, _jsonOptions, ct);

			if ((token = loginResponsePayload?.AccessToken) == null)
			{
				throw new TcClientException(loginResponse.StatusCode, "Invalid login response");
			}
			else
			{
				await _context.SetAuthToken(ct, token);

				request.Headers.Authorization = new AuthenticationHeaderValue(loginResponsePayload?.TokenType ?? "Bearer", token);
				return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
			}
		}

		private HttpContent GetJsonContent(object entity)
		{
			var payload = JsonSerializer.Serialize(entity);
			return new StringContent(payload, _encoding, "application/json");
		}

		public void Dispose()
		{
			_httpClient.Dispose();
		}
	}
}
