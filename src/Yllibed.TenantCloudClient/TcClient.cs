using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

			var httpHandler = new HttpClientHandler()
			{
				UseCookies = false,
				UseDefaultCredentials = false,
				AllowAutoRedirect = true,
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
			};

			_httpClient = new HttpClient(httpHandler, true)
			{
				BaseAddress = new Uri("https://home.tenantcloud.com/v1/")
			};

			_httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Le4007.maison", "0.1"));
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/json"));
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/*"));
		}

		public async Task<TcTenant[]> GetActiveTenants(CancellationToken ct)
		{
			var response = await HttpGet<TcListResponse<TcTenant>>(ct, "landlord/tenants?status=0");
			return response?.Entries;
		}

		public async Task<TcTenantDetails> GetTenantDetails(CancellationToken ct, long tenantId)
		{
			var response = await HttpGet<TcTenantDetails>(ct, $"landlord/tenants/{tenantId}");
			return response;
		}

		public async Task<TcProperty[]> GetProperties(CancellationToken ct)
		{
			var response = await HttpGet<TcListResponse<TcProperty>>(ct, "landlord/property");
			return response?.Entries;
		}

		public async Task<TcUnitDetails[]> GetUnitDetails(CancellationToken ct, long propertyId)
		{
			var response = await HttpGet<TcListResponse<TcUnitDetails>>(ct, $"landlord/property/{propertyId}/units");
			return response?.Entries;
		}

		private async Task<T> HttpGet<T>(CancellationToken ct, string uri)
		{
			var req = new HttpRequestMessage(HttpMethod.Get, uri);
			using (var response = await HttpSend(ct, req))
			{
				var payload = await response.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<T>(payload);
			}
		}

		private async Task<HttpResponseMessage> HttpSend(CancellationToken ct, HttpRequestMessage request)
		{
			var token = await _context.GetAuthToken(ct);

			if (!string.IsNullOrEmpty(token))
			{
				request.Headers.Add("Cookie", $"tc_session={token};");
				var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

				if (response.IsSuccessStatusCode)
				{
					return response;
				}

				request.Headers.Remove("Cookie"); // remove old token from request (will be replaced)
			}

			var loginRequest = new TcLoginRequest(await _context.GetCredentials(ct));
			var loginRequestMsg = new HttpRequestMessage(HttpMethod.Post, "auth/login")
			{
				Content = GetJsonContent(loginRequest)
			};

			var loginResponse =
				await _httpClient.SendAsync(loginRequestMsg, HttpCompletionOption.ResponseHeadersRead, ct);

			loginResponse.EnsureSuccessStatusCode();

			token = loginResponse
				.Headers
				.GetValues("Set-Cookie")
				.SelectMany(v => v
					.Split(';')
					.Select(c => c.Split('='))
					.Where(p => p.Length == 2 && p[0].Equals("tc_session", StringComparison.OrdinalIgnoreCase))
					.Select(p => p[1].Trim()))
				.FirstOrDefault();

			if (token == null)
			{
				throw new InvalidOperationException("Unable to login into TenantCloud.");
			}

			await _context.SetAuthToken(ct, token);

			request.Headers.Add("Cookie", $"tc_session={token};");
			return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
		}

		private HttpContent GetJsonContent(object entity)
		{
			var payload = JsonConvert.SerializeObject(entity);
			return new StringContent(payload, _encoding, "application/json");
		}

		public void Dispose()
		{
			_httpClient.Dispose();
		}
	}
}
