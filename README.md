# Yllibed.TenantCloudClient

Unofficial .NET client library for [TenantCloud](https://tenantcloud.com), a rental property management platform.

[![Build Status](https://dev.azure.com/yllibed/TenantCloudClient/_apis/build/status/yllibed.TenantCloudClient?branchName=master)](https://dev.azure.com/yllibed/TenantCloudClient/_build/latest?definitionId=1&branchName=master) [![Nuget](https://img.shields.io/nuget/dt/Yllibed.TenantCloudClient.svg?label=nuget.org)](https://www.nuget.org/packages/Yllibed.TenantCloudClient)

> **This is not an official TenantCloud product.** TenantCloud does not provide a public API; this library works against their internal endpoints.

## Packages

| Package | Description |
|---------|-------------|
| [`Yllibed.TenantCloudClient`](https://www.nuget.org/packages/Yllibed.TenantCloudClient/) | Core library: API client, token store abstractions, and OS-native secure storage |
| [`Yllibed.TenantCloudClient.Cdp`](https://www.nuget.org/packages/Yllibed.TenantCloudClient.Cdp/) | Chrome DevTools Protocol token provider (extracts tokens from a running browser) |

Both packages target **net8.0** and **net10.0** with no external runtime dependencies beyond `System.Text.Json` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

## Quick start

### With dependency injection

```csharp
services
    .AddSecureTokenStore()      // ITcTokenStore → OS credential store
    .AddCdpTokenProvider()      // ITcAuthTokenProvider → browser extraction + auto-refresh
    .AddTenantCloudClient();    // ITcClient → TcClient
```

Then inject `ITcClient` wherever you need it:

```csharp
public class MyService(ITcClient tc)
{
    public async Task<TcUserInfo?> WhoAmI(CancellationToken ct)
        => await tc.GetUserInfo(ct);
}
```

### Without dependency injection

```csharp
var tokenStore = new SecureTokenStore();
var tokenProvider = new CdpTokenProvider(new CdpTokenProviderOptions
{
    TokenStore = tokenStore,
    AllowInteractiveLogin = true,
});

using var client = new TcClient(tokenProvider);
var user = await client.GetUserInfo(ct);
```

## Authentication

`TcClient` requires an `ITcAuthTokenProvider` to supply Bearer tokens. The library does not store or manage credentials directly.

```csharp
public interface ITcAuthTokenProvider
{
    Task<string?> GetToken(CancellationToken ct);
    Task OnTokenRejected(CancellationToken ct, string rejectedToken);
}
```

### Built-in: `CdpTokenProvider`

Provided by the **Yllibed.TenantCloudClient.Cdp** package. Extracts auth tokens from a running Chromium browser via the Chrome DevTools Protocol, with automatic JWT refresh.

```csharp
services.AddCdpTokenProvider(options =>
{
    options.DebugPort = 9222;               // CDP debug port (default)
    options.AllowInteractiveLogin = true;   // launch a browser if no session found
});
```

The provider follows a multi-step strategy:
1. In-memory cache (if the JWT is still valid)
2. Token store (load + refresh if expired)
3. CDP extraction from an existing browser tab on `app.tenantcloud.com`
4. Interactive login (if `AllowInteractiveLogin` is enabled) — launches a browser window and waits for the user to sign in

### Custom provider

Implement `ITcAuthTokenProvider` and register it before calling `AddTenantCloudClient()`:

```csharp
services.AddSingleton<ITcAuthTokenProvider, MyCustomTokenProvider>();
services.AddTenantCloudClient();
```

## Token persistence

`ITcTokenStore` allows tokens to survive across process restarts. Both the core library and the CDP provider can use it.

```csharp
public interface ITcTokenStore
{
    Task<TcTokenSet?> LoadAsync(CancellationToken ct);
    Task SaveAsync(TcTokenSet tokens, CancellationToken ct);
}
```

### Built-in stores

| Store | Package | Description |
|-------|---------|-------------|
| `SecureTokenStore` | Core | OS-native credential storage: **DPAPI** (Windows), **Keychain** (macOS), **Secret Service** (Linux) |
| `FileTokenStore` | Cdp | Plain JSON file with atomic writes (useful for headless/CI scenarios) |

```csharp
// OS-native secure storage (recommended)
services.AddSecureTokenStore();

// With custom options
services.AddSecureTokenStore(options =>
{
    options.ServiceName = "MyApp";
    options.AccountKey = "production";
});

// Or file-based (from the Cdp package, register manually)
services.AddSingleton<ITcTokenStore>(new FileTokenStore("/path/to/tokens.json"));
```

### Custom store

Implement `ITcTokenStore` to persist tokens wherever you need (database, Azure Key Vault, etc.):

```csharp
services.AddSingleton<ITcTokenStore, MyDatabaseTokenStore>();
```

## API reference

### `ITcClient`

| Member | Type | Description |
|--------|------|-------------|
| `GetUserInfo(ct)` | `Task<TcUserInfo?>` | Current signed-in user info |
| `Contacts` | `IPaginatedSource<TcContact>` | Contacts (tenants, professionals) |
| `Properties` | `IPaginatedSource<TcProperty>` | Properties |
| `Units` | `IPaginatedSource<TcUnit>` | Rental units |
| `Transactions` | `IPaginatedSource<TcTransaction>` | Financial transactions |
| `Leases` | `IPaginatedSource<TcLease>` | Leases |

### Paginated sources

Each collection is an `IPaginatedSource<T>`. Call `.GetAll(ct)` to fetch all pages, or `.GetAll(ct, maxResults: n)` to cap the fetch:

```csharp
var contacts = await client.Contacts.OnlyMovedIn().GetAll(ct);
```

The result is a `ReadOnlySequence<T>` (one segment per API page). Use `.AsEnumerable()` to bridge to LINQ:

```csharp
var names = (await client.Contacts.GetAll(ct))
    .AsEnumerable()
    .Select(c => c.Name)
    .ToArray();
```

### Filters

Filters are chainable extension methods that narrow the API query before fetching.

**Contacts**
- `.OnlyTenants()` — tenant contacts only
- `.OnlyMovedIn()` — tenants with active leases
- `.OnlyProfessionals()` — professional contacts only
- `.OnlyArchived()` — archived contacts

**Leases**
- `.OnlyActive()` — active leases
- `.ForProperty(propertyId)` — filter by property
- `.ForUnit(unitId)` — filter by unit

**Units**
- `.OnlyOccuped()` — units with active leases
- `.OnlyVacant()` — vacant units
- `.ForProperty(propertyId)` — filter by property

**Transactions**
- `.ForTenant(tenantId)` — filter by tenant
- `.ForProperty(propertyId)` — filter by property
- `.ForUnit(unitId)` — filter by unit
- `.ForStatus(TcTransactionStatus)` — filter by status (`Due`, `Paid`, `Partial`, `Pending`, `Void`, `WithBalance`, `Overdue`, `Waive`)
- `.ForCategory(TcTransactionCategory)` — filter by category (`Income`, `Expense`, `Refund`, `Credits`, `Liability`)
- `.SortByDateDescending()` — reverse chronological order

### Example: overdue income per property

```csharp
var balancePerProperty = (await client.Transactions
        .ForCategory(TcTransactionCategory.Income)
        .ForStatus(TcTransactionStatus.WithBalance)
        .GetAll(ct))
    .AsEnumerable()
    .Where(t => t.PropertyId != null)
    .GroupBy(t => (long)t.PropertyId!, t => t.Balance)
    .Select(g => new { PropertyId = g.Key, Balance = g.Sum() })
    .ToArray();
```

## License

MIT
