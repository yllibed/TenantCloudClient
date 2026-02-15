using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Yllibed.TenantCloudClient.Cdp.CdpMessages;

namespace Yllibed.TenantCloudClient.Cdp;

internal static class TcTokenRefresher
{
	public static async Task<TcTokenSet?> RefreshAsync(
		TcTokenSet current,
		string apiUrl,
		CancellationToken ct)
	{
		try
		{
			using var http = new HttpClient();
			var requestBody = new TcRefreshRequest
			{
				GrantType = "refresh_token",
				Fingerprint = current.Fingerprint,
				RefreshToken = current.RefreshToken,
			};

			var json = JsonSerializer.Serialize(requestBody, CdpJsonContext.Default.TcRefreshRequest);
			using var content = new StringContent(json, Encoding.UTF8, "application/json");

			using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl.TrimEnd('/')}/auth/token")
			{
				Content = content,
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.AccessToken);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			request.Headers.Add("X-Requested-With", "XMLHttpRequest");

			using var response = await http.SendAsync(request, ct).ConfigureAwait(false);

			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
			var result = JsonSerializer.Deserialize(responseJson, CdpJsonContext.Default.TcRefreshResponse);

			if (result?.AccessToken is null || result.RefreshToken is null)
			{
				return null;
			}

			return new TcTokenSet(result.AccessToken, result.RefreshToken, current.Fingerprint);
		}
		catch (Exception) when (!ct.IsCancellationRequested)
		{
			return null;
		}
	}
}
