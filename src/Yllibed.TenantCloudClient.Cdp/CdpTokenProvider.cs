using System.Diagnostics;
using System.Text.Json;
using Yllibed.TenantCloudClient.Cdp.CdpMessages;

namespace Yllibed.TenantCloudClient.Cdp;

/// <summary>
/// Token provider that connects to a Chromium browser via Chrome DevTools Protocol
/// to extract TenantCloud auth tokens from an existing browser session,
/// with automatic refresh and optional interactive login.
/// </summary>
public sealed class CdpTokenProvider : ITcAuthTokenProvider, IDisposable
{
	private readonly CdpTokenProviderOptions _options;
	private readonly SemaphoreSlim _gate = new(1, 1);

	private TcTokenSet? _cached;
	private Process? _browserProcess;

	public CdpTokenProvider(CdpTokenProviderOptions? options = null)
	{
		_options = options ?? new CdpTokenProviderOptions();
	}

	/// <inheritdoc />
	public async Task<string?> GetToken(CancellationToken ct)
	{
		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			return await GetTokenCoreAsync(ct).ConfigureAwait(false);
		}
		finally
		{
			_gate.Release();
		}
	}

	/// <inheritdoc />
	public async Task OnTokenRejected(CancellationToken ct, string rejectedToken)
	{
		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			if (_cached is not null
				&& string.Equals(_cached.AccessToken, rejectedToken, StringComparison.Ordinal))
			{
				var refreshed = await TcTokenRefresher.RefreshAsync(
					_cached, _options.TenantCloudApiUrl, ct).ConfigureAwait(false);

				if (refreshed is not null)
				{
					_cached = refreshed;
					await PersistAsync(refreshed, ct).ConfigureAwait(false);
				}
				else
				{
					_cached = null;
				}
			}
		}
		catch
		{
			_cached = null;
		}
		finally
		{
			_gate.Release();
		}
	}

	private async Task<string?> GetTokenCoreAsync(CancellationToken ct)
	{
		// Step 1: In-memory cache — return if not expired
		if (_cached is not null && !IsExpiredOrExpiring(_cached.AccessToken))
		{
			return _cached.AccessToken;
		}

		// Step 2: Token store — load, then try refresh if expired
		var tokens = await TryLoadAndRefreshFromStoreAsync(ct).ConfigureAwait(false);
		if (tokens is not null)
		{
			_cached = tokens;
			return tokens.AccessToken;
		}

		// Step 3: CDP extraction from an existing browser
		tokens = await TryExtractFromBrowserAsync(_options.DebugPort, ct).ConfigureAwait(false);
		if (tokens is not null)
		{
			_cached = tokens;
			await PersistAsync(tokens, ct).ConfigureAwait(false);
			return tokens.AccessToken;
		}

		// Step 4: Interactive login (if allowed)
		if (_options.AllowInteractiveLogin)
		{
			tokens = await TryInteractiveLoginAsync(ct).ConfigureAwait(false);
			if (tokens is not null)
			{
				_cached = tokens;
				await PersistAsync(tokens, ct).ConfigureAwait(false);
				return tokens.AccessToken;
			}
		}

		// Step 5: Nothing worked
		return null;
	}

	private async Task<TcTokenSet?> TryLoadAndRefreshFromStoreAsync(CancellationToken ct)
	{
		if (_options.TokenStore is null)
		{
			return null;
		}

		try
		{
			var stored = await _options.TokenStore.LoadAsync(ct).ConfigureAwait(false);
			if (stored is null)
			{
				return null;
			}

			if (!IsExpiredOrExpiring(stored.AccessToken))
			{
				return stored;
			}

			var refreshed = await TcTokenRefresher.RefreshAsync(
				stored, _options.TenantCloudApiUrl, ct).ConfigureAwait(false);

			if (refreshed is not null)
			{
				await PersistAsync(refreshed, ct).ConfigureAwait(false);
			}

			return refreshed;
		}
		catch
		{
			return null;
		}
	}

	private async Task<TcTokenSet?> TryExtractFromBrowserAsync(int port, CancellationToken ct)
	{
		try
		{
			var wsUrl = await CdpBrowserDiscovery.FindTenantCloudTargetAsync(
				port, _options.TenantCloudAppUrl, _options.DiscoveryTimeout, ct).ConfigureAwait(false);

			if (wsUrl is null)
			{
				return null;
			}

			return await ExtractTokensViaCdpAsync(wsUrl, ct).ConfigureAwait(false);
		}
		catch
		{
			return null;
		}
	}

	private async Task<TcTokenSet?> ExtractTokensViaCdpAsync(Uri wsUrl, CancellationToken ct)
	{
		var connection = new CdpConnection();
		await using (connection.ConfigureAwait(false))
		{
			await connection.ConnectAsync(wsUrl, _options.WebSocketTimeout, ct).ConfigureAwait(false);

			var accessToken = await EvaluateStringAsync(
				connection, "JSON.parse(localStorage.getItem('access_token'))", ct).ConfigureAwait(false);

			var fingerprint = await EvaluateStringAsync(
				connection, "JSON.parse(localStorage.getItem('fingerprint'))", ct).ConfigureAwait(false);

			var refreshToken = await ExtractRefreshTokenCookieAsync(connection, ct).ConfigureAwait(false);

			if (accessToken is null || fingerprint is null || refreshToken is null)
			{
				return null;
			}

			return new TcTokenSet(accessToken, refreshToken, fingerprint);
		}
	}

	private static async Task<string?> EvaluateStringAsync(
		CdpConnection connection, string expression, CancellationToken ct)
	{
		try
		{
			var evalParams = new CdpEvaluateParams { Expression = expression, ReturnByValue = true };
			var paramsElement = JsonSerializer.SerializeToElement(
				evalParams, CdpJsonContext.Default.CdpEvaluateParams);

			var result = await connection.SendCommandAsync(
				"Runtime.evaluate",
				paramsElement,
				CdpJsonContext.Default.CdpEvaluateResult,
				ct).ConfigureAwait(false);

			if (result?.ExceptionDetails is not null || result?.Result is null)
			{
				return null;
			}

			return result.Result.Value?.ToString();
		}
		catch
		{
			return null;
		}
	}

	private async Task<string?> ExtractRefreshTokenCookieAsync(
		CdpConnection connection, CancellationToken ct)
	{
		try
		{
			var cookieParams = new CdpGetCookiesParams
			{
				Urls = [_options.TenantCloudApiUrl, _options.TenantCloudAppUrl],
			};
			var paramsElement = JsonSerializer.SerializeToElement(
				cookieParams, CdpJsonContext.Default.CdpGetCookiesParams);

			var result = await connection.SendCommandAsync(
				"Network.getCookies",
				paramsElement,
				CdpJsonContext.Default.CdpGetCookiesResult,
				ct).ConfigureAwait(false);

			if (result?.Cookies is null)
			{
				return null;
			}

			foreach (var cookie in result.Cookies)
			{
				if (string.Equals(cookie.Name, "tc_refresh_token", StringComparison.Ordinal)
					&& !string.IsNullOrEmpty(cookie.Value))
				{
					return cookie.Value;
				}
			}

			return null;
		}
		catch
		{
			return null;
		}
	}

	private async Task<TcTokenSet?> TryInteractiveLoginAsync(CancellationToken ct)
	{
		try
		{
			var browserPath = ResolveBrowserPath();
			if (browserPath is null)
			{
				return null;
			}

			var port = Random.Shared.Next(10000, 60000);
			var tempProfile = Path.Combine(Path.GetTempPath(), $"tc-cdp-{port}");

			try
			{
				return await LaunchBrowserAndExtractTokensAsync(
					browserPath, port, tempProfile, ct).ConfigureAwait(false);
			}
			finally
			{
				KillBrowserProcess();
				TryDeleteDirectory(tempProfile);
			}
		}
		catch
		{
			return null;
		}
	}

	private string? ResolveBrowserPath()
	{
		if (_options.BrowserExecutablePath is not null)
		{
			return _options.BrowserExecutablePath;
		}

		var browsers = ChromiumFinder.FindAll();
		return browsers.Count > 0 ? browsers[0].ExecutablePath : null;
	}

	private async Task<TcTokenSet?> LaunchBrowserAndExtractTokensAsync(
		string browserPath, int port, string tempProfile, CancellationToken ct)
	{
		Directory.CreateDirectory(tempProfile);

		var loginUrl = $"{_options.TenantCloudAppUrl.TrimEnd('/')}/login";

		_browserProcess = Process.Start(new ProcessStartInfo
		{
			FileName = browserPath,
			ArgumentList =
			{
				$"--remote-debugging-port={port}",
				$"--app={loginUrl}",
				$"--user-data-dir={tempProfile}",
				"--no-first-run",
				"--disable-extensions",
			},
			UseShellExecute = false,
		});

		if (_browserProcess is null || _browserProcess.HasExited)
		{
			return null;
		}

		// Wait for the browser to start CDP
		await Task.Delay(2000, ct).ConfigureAwait(false);

		return await PollForLoginCompletionAsync(port, ct).ConfigureAwait(false);
	}

	private async Task<TcTokenSet?> PollForLoginCompletionAsync(int port, CancellationToken ct)
	{
		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeoutCts.CancelAfter(TimeSpan.FromMinutes(5));

		while (!timeoutCts.Token.IsCancellationRequested)
		{
			await Task.Delay(1500, timeoutCts.Token).ConfigureAwait(false);

			try
			{
				var tokens = await TryExtractAfterLoginAsync(port, timeoutCts.Token)
					.ConfigureAwait(false);
				if (tokens is not null)
				{
					return tokens;
				}
			}
			catch (OperationCanceledException)
			{
				return null;
			}
			catch
			{
				// CDP not ready yet, keep polling
			}
		}

		return null;
	}

	private async Task<TcTokenSet?> TryExtractAfterLoginAsync(int port, CancellationToken ct)
	{
		var wsUrl = await CdpBrowserDiscovery.FindAnyTargetAsync(
			port, _options.DiscoveryTimeout, ct).ConfigureAwait(false);

		if (wsUrl is null)
		{
			return null;
		}

		var connection = new CdpConnection();
		await using (connection.ConfigureAwait(false))
		{
			await connection.ConnectAsync(wsUrl, _options.WebSocketTimeout, ct).ConfigureAwait(false);

			var currentUrl = await EvaluateStringAsync(
				connection, "window.location.href", ct).ConfigureAwait(false);

			if (currentUrl is null)
			{
				return null;
			}

			// Still on login or 2FA page — keep waiting
			if (currentUrl.Contains("/login", StringComparison.OrdinalIgnoreCase)
				|| currentUrl.Contains("/two_factor", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			// User has navigated past login — extract tokens
			return await ExtractTokensViaCdpAsync(wsUrl, ct).ConfigureAwait(false);
		}
	}

	private async Task PersistAsync(TcTokenSet tokens, CancellationToken ct)
	{
		if (_options.TokenStore is not null)
		{
			try
			{
				await _options.TokenStore.SaveAsync(tokens, ct).ConfigureAwait(false);
			}
			catch
			{
				// Persistence failure is non-fatal
			}
		}
	}

	private static bool IsExpiredOrExpiring(string? accessToken)
	{
		var expiry = JwtHelper.GetExpiry(accessToken);
		if (expiry is null)
		{
			return true;
		}

		return expiry.Value < DateTimeOffset.UtcNow.AddSeconds(60);
	}

	private void KillBrowserProcess()
	{
		try
		{
			if (_browserProcess is { HasExited: false })
			{
				_browserProcess.Kill(entireProcessTree: true);
			}
		}
		catch
		{
			// Best effort
		}
		finally
		{
			_browserProcess?.Dispose();
			_browserProcess = null;
		}
	}

	private static void TryDeleteDirectory(string path)
	{
		try
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
		}
		catch
		{
			// Best effort — temp profile cleanup
		}
	}

	public void Dispose()
	{
		KillBrowserProcess();
		_gate.Dispose();
	}
}
