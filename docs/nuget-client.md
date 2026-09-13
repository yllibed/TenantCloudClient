# Yllibed.TenantCloudClient

Unofficial .NET client library for [TenantCloud](https://tenantcloud.com), a rental property management platform.

> **This is not an official TenantCloud product.** TenantCloud does not provide a public API; this library works against their internal endpoints.

## Quick start

Targets .NET 8 and .NET 10. Install `Yllibed.TenantCloudClient.Cdp` as well for browser authentication. DI examples need `Microsoft.Extensions.DependencyInjection` and imports for that namespace, `Yllibed.TenantCloudClient`, `Yllibed.TenantCloudClient.Cdp` and `Yllibed.TenantCloudClient.HttpMessages`.

### With dependency injection

```csharp
services
    .AddSecureTokenStore()      // ITcTokenStore → OS credential store
    .AddCdpTokenProvider()      // ITcAuthTokenProvider (from Yllibed.TenantCloudClient.Cdp)
    .AddTenantCloudClient();    // ITcClient → TcClient
```

Then inject `ITcClient`:

```csharp
public class MyService(ITcClient tc)
{
    public async Task<TcUserInfo?> WhoAmI(CancellationToken ct)
        => await tc.GetUserInfo(ct);
}
```

The default CDP provider reuses stored tokens or a browser with CDP enabled. For first-time interactive login, configure a provider as below.

### Without dependency injection

```csharp
using var tokenProvider = new CdpTokenProvider(new CdpTokenProviderOptions
{
    TokenStore = new SecureTokenStore(),
    AllowInteractiveLogin = true,
});

using var client = new TcClient(tokenProvider);
var user = await client.GetUserInfo(CancellationToken.None);
```

## API

| Member | Type | Description |
|--------|------|-------------|
| `GetUserInfo(ct)` | `Task<TcUserInfo?>` | Current signed-in user |
| `Contacts` | `IPaginatedSource<TcContact>` | Contacts (tenants, professionals) |
| `Properties` | `IPaginatedSource<TcProperty>` | Properties |
| `Units` | `IPaginatedSource<TcUnit>` | Rental units |
| `Transactions` | `IPaginatedSource<TcTransaction>` | Financial transactions |
| `Leases` | `IPaginatedSource<TcLease>` | Leases |

Filters: `.OnlyTenants()`, `.OnlyActive()`, `.ForProperty(id)`, `.ForStatus(...)`, and more.

```csharp
var tenants = await client.Contacts.OnlyTenants().GetAll(ct);
```

`GetAll` has a default target of 300 results and may exceed `maxResults` by including whole API pages. It is not an unlimited fetch or a strict cap. Use `GetPage` for explicit traversal and truncate locally when necessary.

## Migrating to v3

The v2 `ITcContext` login and `Tenants` collection have been replaced by token providers and `Contacts`. `TcProperty` exposes fields directly rather than through `Attributes`, and `TcUnit.Price` is now `decimal?`: null means unknown, not zero. See the [migration guide](https://github.com/yllibed/TenantCloudClient/blob/master/docs/client-library.md#migrating-to-v3).

Archived-contact filtering remains unconfirmed. See [known limitations](https://github.com/yllibed/TenantCloudClient/blob/master/docs/client-library.md#known-limitations) for the open follow-ups in issue #12.

For full documentation, see the [GitHub repository](https://github.com/yllibed/TenantCloudClient).
