# Client Library

The `Yllibed.TenantCloudClient` and `Yllibed.TenantCloudClient.Cdp` NuGet packages provide programmatic access to TenantCloud data.

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
