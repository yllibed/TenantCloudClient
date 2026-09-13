# Yllibed.TenantCloudClient.Cdp

Chrome DevTools Protocol (CDP) based token provider for [Yllibed.TenantCloudClient](https://www.nuget.org/packages/Yllibed.TenantCloudClient/). Extracts auth tokens from an existing browser session with automatic JWT refresh.

## Quick start

Targets .NET 8 and .NET 10. DI examples assume a `ServiceCollection` and imports for `Microsoft.Extensions.DependencyInjection`, `Yllibed.TenantCloudClient` and `Yllibed.TenantCloudClient.Cdp`.

```csharp
services
    .AddSecureTokenStore()      // Persist tokens in OS credential store
    .AddCdpTokenProvider()      // Extract tokens from browser via CDP
    .AddTenantCloudClient();    // Register ITcClient
```

## Options

Interactive login is disabled by default. To enable it, register a configured provider instead of calling `AddCdpTokenProvider()`:

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

Options have `init` properties and must be configured with an object initializer. A null executable path enables automatic Chromium discovery.

## How it works

1. **In-memory cache** — returns the cached JWT if still valid
2. **Token store** — loads persisted tokens and refreshes if expired
3. **CDP extraction** — connects to a running Chromium browser and extracts tokens from a TenantCloud tab
4. **Interactive login** — launches a temporary browser window for sign-in (if enabled)

No external NuGet dependencies beyond the base client library.

## Prerequisites

Use a supported Chromium browser, not Firefox or Safari. An existing browser must expose a local CDP endpoint; an ordinary signed-in window is not enough. Interactive login opens a separate temporary profile and requires a graphical session.

Secure storage uses DPAPI-encrypted files on Windows, Keychain on macOS and `secret-tool` with an unlocked Secret Service on Linux. See [authentication prerequisites](https://github.com/yllibed/TenantCloudClient/blob/master/docs/authentication.md) before using a headless environment. Keep CDP endpoints local and never share token files or raw authentication logs.

For full documentation, see the [GitHub repository](https://github.com/yllibed/TenantCloudClient).
