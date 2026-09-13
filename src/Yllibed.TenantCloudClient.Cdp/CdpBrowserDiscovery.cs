using System.Globalization;

namespace Yllibed.TenantCloudClient.Cdp;

internal static class CdpBrowserDiscovery
{
	public static async Task<int> WaitForReadyAsync(
		string profileDirectory, Func<bool> hasExited, TimeSpan timeout, CancellationToken ct)
	{
		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeoutCts.CancelAfter(timeout);
		var portFile = Path.Combine(profileDirectory, "DevToolsActivePort");

		try
		{
			while (true)
			{
				timeoutCts.Token.ThrowIfCancellationRequested();
				if (hasExited())
				{
					throw new InvalidOperationException("The browser exited before its CDP endpoint was ready.");
				}

				string[] lines = [];
				try
				{
					lines = await File.ReadAllLinesAsync(portFile, timeoutCts.Token).ConfigureAwait(false);
				}
				catch (IOException)
				{
					// Chromium may still be creating or writing the port file.
				}

				if (lines.Length >= 2
					&& int.TryParse(lines[0], NumberStyles.None, CultureInfo.InvariantCulture, out var port)
					&& port is > 0 and <= 65535
					&& lines[1].StartsWith("/devtools/browser/", StringComparison.Ordinal)
					&& await FindAnyTargetAsync(port, timeout, timeoutCts.Token).ConfigureAwait(false) is not null)
				{
					return port;
				}

				await Task.Delay(100, timeoutCts.Token).ConfigureAwait(false);
			}
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			throw new TimeoutException("The browser did not expose a working CDP endpoint before the startup timeout.");
		}
	}

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
