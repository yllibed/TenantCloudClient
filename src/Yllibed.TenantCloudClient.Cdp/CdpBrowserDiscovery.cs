namespace Yllibed.TenantCloudClient.Cdp;

internal static class CdpBrowserDiscovery
{
	public static async Task<Uri?> FindTenantCloudTargetAsync(
		int port,
		string appUrl,
		TimeSpan timeout,
		CancellationToken ct)
	{
		try
		{
			using var http = new HttpClient { Timeout = timeout };
			var json = await http.GetStringAsync($"http://localhost:{port}/json", ct).ConfigureAwait(false);

			var targets = JsonSerializer.Deserialize(json, CdpJsonContext.Default.CdpTargetArray);
			if (targets is null)
			{
				return null;
			}

			var appHost = new Uri(appUrl).Host;

			foreach (var target in targets)
			{
				if (!string.Equals(target.Type, "page", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (string.IsNullOrEmpty(target.Url) || string.IsNullOrEmpty(target.WebSocketDebuggerUrl))
				{
					continue;
				}

				if (Uri.TryCreate(target.Url, UriKind.Absolute, out var targetUri)
					&& targetUri.Host.EndsWith(appHost, StringComparison.OrdinalIgnoreCase))
				{
					return new Uri(target.WebSocketDebuggerUrl);
				}
			}

			return null;
		}
		catch (Exception) when (ct.IsCancellationRequested)
		{
			return null;
		}
		catch (HttpRequestException)
		{
			return null;
		}
		catch (TaskCanceledException)
		{
			return null;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	public static async Task<Uri?> FindAnyTargetAsync(
		int port,
		TimeSpan timeout,
		CancellationToken ct)
	{
		try
		{
			using var http = new HttpClient { Timeout = timeout };
			var json = await http.GetStringAsync($"http://localhost:{port}/json", ct).ConfigureAwait(false);

			var targets = JsonSerializer.Deserialize(json, CdpJsonContext.Default.CdpTargetArray);
			if (targets is null)
			{
				return null;
			}

			foreach (var target in targets)
			{
				if (string.Equals(target.Type, "page", StringComparison.OrdinalIgnoreCase)
					&& !string.IsNullOrEmpty(target.WebSocketDebuggerUrl))
				{
					return new Uri(target.WebSocketDebuggerUrl);
				}
			}

			return null;
		}
		catch
		{
			return null;
		}
	}
}
