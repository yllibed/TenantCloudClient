# Authentication

TenantCloud does not offer a public OAuth or API-key flow. This library authenticates by extracting JWT tokens from a running browser session via the Chrome DevTools Protocol (CDP).

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
}
```

### `SecureTokenStore` (recommended)

Uses the OS-native credential store:

| OS | Backend |
|----|---------|
| Windows | DPAPI (`CredWrite` / `CredRead`) |
| macOS | Keychain (`security` CLI) |
| Linux | Secret Service D-Bus API |

```csharp
services.AddSecureTokenStore();

// With custom options
services.AddSecureTokenStore(options =>
{
    options.ServiceName = "MyApp";
    options.AccountKey = "production";
});
```

### `FileTokenStore`

Plain JSON file with atomic writes. Useful for headless/CI scenarios where no credential store is available:

```csharp
services.AddSingleton<ITcTokenStore>(new FileTokenStore("/path/to/tokens.json"));
```

> **Security note**: the file contains sensitive refresh tokens. Protect it with appropriate file permissions.

### Custom store

Implement `ITcTokenStore` to persist tokens wherever you need (database, Azure Key Vault, etc.):

```csharp
services.AddSingleton<ITcTokenStore, MyDatabaseTokenStore>();
```

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
services.AddCdpTokenProvider(options =>
{
    options.DebugPort = 9222;
    options.AllowInteractiveLogin = true;
});
```

### Custom provider

```csharp
services.AddSingleton<ITcAuthTokenProvider, MyCustomTokenProvider>();
services.AddTenantCloudClient();
```
