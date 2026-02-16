# Yllibed.TenantCloudClient.Cdp

Chrome DevTools Protocol (CDP) based token provider for [Yllibed.TenantCloudClient](https://www.nuget.org/packages/Yllibed.TenantCloudClient/). Extracts auth tokens from an existing browser session with automatic JWT refresh.

## Quick start

```csharp
services
    .AddSecureTokenStore()      // Persist tokens in OS credential store
    .AddCdpTokenProvider()      // Extract tokens from browser via CDP
    .AddTenantCloudClient();    // Register ITcClient
```

## Options

```csharp
services.AddCdpTokenProvider(options =>
{
    options.DebugPort = 9222;               // CDP debug port (default)
    options.AllowInteractiveLogin = true;   // Launch browser if no session found
    options.BrowserExecutablePath = null;   // Auto-detect Chromium browser
});
```

## How it works

1. **In-memory cache** — returns the cached JWT if still valid
2. **Token store** — loads persisted tokens and refreshes if expired
3. **CDP extraction** — connects to a running Chromium browser and extracts tokens from a TenantCloud tab
4. **Interactive login** — launches a temporary browser window for sign-in (if enabled)

No external NuGet dependencies beyond the base client library.

For full documentation, see the [GitHub repository](https://github.com/yllibed/TenantCloudClient).
