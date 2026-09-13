# Authentication

This library authenticates by extracting tokens from a TenantCloud browser session via the Chrome DevTools Protocol (CDP), then refreshing and storing them. It does not submit your username and password directly to the old login endpoint.

## Browser prerequisites

- Use a Chromium browser: Chrome, Edge, Brave or Chromium; Vivaldi is also detected on Windows and macOS. Firefox and Safari are not supported by this provider.
- An already-open browser is usable only if it exposes a local CDP debugging endpoint at the configured `DebugPort` (default 9222) and has a TenantCloud tab open. Being signed in to an ordinary browser window is not sufficient.
- Interactive login uses a separate temporary browser profile and a browser-assigned debugging port. Complete sign-in, including any CAPTCHA or MFA, in that window. The temporary browser is closed after token extraction or failure; your normal browser session is not the target of cleanup.
- Browser sign-in requires a graphical session. Do not expose a CDP port to the network: it grants access to browser session data.

For the CLI, run `tenantcloud login`. Library consumers must opt into interactive login as shown below; it is disabled by default.

## How the CDP flow works

1. **Browser extraction** — connects to a Chromium browser's CDP debug port, looks for a tab on `app.tenantcloud.com`, and reads `access_token` + `fingerprint` from `localStorage` and `tc_refresh_token` from cookies
2. **Token refresh** — when the JWT expires, the library calls TenantCloud's refresh endpoint with the refresh token and fingerprint
3. **Persist & loop** — refreshed tokens are saved to the configured `ITcTokenStore` for next launch

### Interactive login

If `AllowInteractiveLogin` is enabled and no existing session is found, the provider launches a temporary Chromium instance pointing to the TenantCloud login page. Once you sign in, tokens are extracted automatically.

## Token persistence — `ITcTokenStore`

Tokens can survive across process restarts via `ITcTokenStore`:

```csharp
public interface ITcTokenStore
{
    Task<TcTokenSet?> LoadAsync(CancellationToken ct);
    Task SaveAsync(TcTokenSet tokens, CancellationToken ct);
    Task DeleteAsync(CancellationToken ct);
}
```

### `SecureTokenStore` (recommended)

Uses OS-backed storage:

| OS | Backend |
|----|---------|
| Windows | DPAPI-encrypted files in `%LOCALAPPDATA%/Yllibed/TenantCloud/` |
| macOS | Keychain (`security` CLI) |
| Linux | Secret Service through the `secret-tool` CLI |

Windows tokens are tied to the current user's DPAPI context. On macOS, the user's Keychain must be accessible. On Linux, install `secret-tool` and provide an accessible, unlocked Secret Service in the user's session; a minimal container or headless runner may not have one. `SecureTokenStore.IsSupported` identifies the OS, not whether these services are available.

Examples using `services` assume a `ServiceCollection` and imports for `Microsoft.Extensions.DependencyInjection`, `Yllibed.TenantCloudClient` and `Yllibed.TenantCloudClient.Cdp`.

```csharp
services.AddSecureTokenStore();

// With custom options
services.AddSingleton<ITcTokenStore>(new SecureTokenStore(new SecureTokenStoreOptions
{
    ServiceName = "MyApp",
    AccountKey = "production",
}));
```

### `FileTokenStore`

Plain JSON file with atomic writes, provided by the CDP package. An option for a library application with provisioned tokens where no credential store is available; it does not remove the need to authenticate or automatically configure the `tenantcloud` tool:

```csharp
services.AddSingleton<ITcTokenStore>(new FileTokenStore("/path/to/tokens.json"));
```

> **Security note**: the file contains sensitive refresh tokens. Protect it with appropriate file permissions.

### Custom store

Implement `LoadAsync`, `SaveAsync` and `DeleteAsync` on `ITcTokenStore`, then register your implementation with dependency injection. Protect refresh tokens in transit and at rest.

## Token provider — `ITcAuthTokenProvider`

The token provider is responsible for supplying Bearer tokens to `TcClient`:

```csharp
public interface ITcAuthTokenProvider
{
    Task<string?> GetToken(CancellationToken ct);
    Task OnTokenRejected(CancellationToken ct, string rejectedToken);
}
```

### Built-in: `CdpTokenProvider`

Provided by the `Yllibed.TenantCloudClient.Cdp` package. Multi-step strategy:

1. In-memory cache (if the JWT is still valid)
2. Token store (load + refresh if expired)
3. CDP extraction from an existing browser tab
4. Interactive login (if `AllowInteractiveLogin` is enabled)

```csharp
services.AddSecureTokenStore();
services.AddSingleton<ITcAuthTokenProvider>(sp => new CdpTokenProvider(new CdpTokenProviderOptions
{
    TokenStore = sp.GetRequiredService<ITcTokenStore>(),
    DebugPort = 9222,
    AllowInteractiveLogin = true,
    BrowserExecutablePath = null,
}));
services.AddTenantCloudClient();
```

`BrowserExecutablePath = null` enables automatic discovery; provide an absolute executable path to select a browser. Options use `init` properties, so configure them with object initializers rather than assignments inside an `Action` callback. Use this registration instead of `AddCdpTokenProvider()` when customizing these options.

### Custom provider

Implement the interface above and register it as `ITcAuthTokenProvider`, followed by `AddTenantCloudClient()`. On rejection, discard or refresh the rejected token; returning the same rejected token does not enable a successful retry.
