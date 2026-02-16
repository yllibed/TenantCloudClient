# Yllibed.TenantCloudClient

Unofficial .NET client library for [TenantCloud](https://tenantcloud.com), a rental property management platform.

> **This is not an official TenantCloud product.** TenantCloud does not provide a public API; this library works against their internal endpoints.

## Quick start

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

### Without dependency injection

```csharp
var tokenProvider = new CdpTokenProvider(new CdpTokenProviderOptions
{
    TokenStore = new SecureTokenStore(),
    AllowInteractiveLogin = true,
});

using var client = new TcClient(tokenProvider);
var user = await client.GetUserInfo(ct);
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

For full documentation, see the [GitHub repository](https://github.com/yllibed/TenantCloudClient).
