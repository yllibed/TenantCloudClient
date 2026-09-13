# Client Library

The `Yllibed.TenantCloudClient` and `Yllibed.TenantCloudClient.Cdp` NuGet packages provide programmatic access to TenantCloud data.

Both packages target **net8.0** and **net10.0**. The core library depends on `Microsoft.Extensions.DependencyInjection.Abstractions` and, on .NET 8, `System.Text.Json`. The CDP package references the core library. Browser and OS storage prerequisites are described in [Authentication](authentication.md).

## Quick start

### With dependency injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Yllibed.TenantCloudClient;
using Yllibed.TenantCloudClient.Cdp;
using Yllibed.TenantCloudClient.HttpMessages;

var services = new ServiceCollection();
services
    .AddSecureTokenStore()
    .AddCdpTokenProvider()
    .AddTenantCloudClient();
```

Install both TenantCloud packages and `Microsoft.Extensions.DependencyInjection` for this standalone DI example. The default provider reuses stored tokens or a browser with CDP enabled; it does not launch a browser. For interactive sign-in, use the [configured provider example](authentication.md#built-in-cdptokenprovider).

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
using Yllibed.TenantCloudClient;
using Yllibed.TenantCloudClient.Cdp;

using var tokenProvider = new CdpTokenProvider(new CdpTokenProviderOptions
{
    TokenStore = new SecureTokenStore(),
    AllowInteractiveLogin = true,
});

using var client = new TcClient(tokenProvider);
var user = await client.GetUserInfo(CancellationToken.None);
```

## Migrating to v3

Compared with the last stable v2.1 release:

| v2 API or requirement | v3 migration |
|-----------------------|--------------|
| .NET Standard 2.1 | Target .NET 8 or .NET 10 |
| `TcClient(ITcContext)`, `InMemoryTcContext`, username/password login | Supply an `ITcAuthTokenProvider`; use the CDP package for browser sign-in and token refresh |
| `ITcClient.Tenants`, `TcTenantDetails` | Use `Contacts` and `TcContact`; apply `OnlyTenants()` when tenant contacts are required |
| `TcProperty.Attributes` | Read `Name`, `Address1`, `CityAddress` and `Status` directly from the property |
| `TcTransactionCategory.liability` | Use `TcTransactionCategory.Liability` |

Review code that serializes models or relies on TenantCloud's old field layout: the client now reads the current JSON:API endpoints, and the model shapes have changed. Lease access is available through `ITcClient.Leases`.

`TcUnit.Price` is now `decimal?` rather than `decimal`, because TenantCloud can
return a missing price. Handle `null` explicitly in calculations and display;
it does not mean a price of zero. JSON nulls and empty price strings become
`null`; numbers and numeric strings are read using invariant culture.

`TcProperty.Status` is `string?` and also accepts numeric or boolean JSON
values. Numeric text is preserved without conversion through floating point.

For integrations using a previous MCP prerelease, see the separate [MCP contract migration](mcp-server.md#pagination-and-migration-from-the-previous-mcp-contract).

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

Each collection is an `IPaginatedSource<T>`. `GetAll(ct)` requests up to a default target of 300 results; it does **not** mean an unlimited fetch. The current implementation appends whole API pages and can exceed `maxResults`, including when the target lands exactly on a page boundary ([#12](https://github.com/yllibed/TenantCloudClient/issues/12)). Do not use this parameter as a strict result or request budget.

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

For an explicit page, use `GetPage`:

```csharp
var (entries, pageNo, totalEntries) = await client.Contacts.OnlyTenants().GetPage(ct, pageNo: 1);
```

To traverse a collection, request subsequent page numbers, stopping when a page is empty or the cumulative number of entries reaches the reported total. Apply any strict result cap to the entries you consume. TenantCloud data can change between requests, so pagination is not a snapshot. The CLI/MCP cursor adapter uses `GetPage` and enforces its own page size independently of `GetAll`.

### Filters

Filters are chainable extension methods that narrow the API query before fetching.

**Contacts**

- `.OnlyTenants()` — tenant contacts only
- `.OnlyMovedIn()` — tenants with active leases
- `.OnlyProfessionals()` — professional contacts only
- `.OnlyArchived()` — sends the archived-status filter; upstream behavior is not yet confirmed (see [known limitations](#known-limitations))

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

### Example: outstanding income in a retrieved batch

This groups the retrieved batch, not necessarily the entire account. For a complete report, traverse the source with `GetPage` as described above. `WithBalance` means an outstanding balance, not necessarily an overdue transaction.

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

## Known limitations

The following follow-ups from [#12](https://github.com/yllibed/TenantCloudClient/issues/12) are accepted limitations for the planned 3.0 stable release; they are not reported as fixed:

- `GetAll(maxResults)` may exceed its target. Use explicit pages and truncate locally when a strict cap matters.
- `OnlyArchived()` / MCP `role=archived` sends `filter[status]=archived`, but whether TenantCloud honors that filter still needs validation on an account with known archived contacts. Do not rely on it for archive-specific reporting yet.
- MCP failures may still lack useful diagnostic detail. HTTP status and JSON paths are not guaranteed to reach the MCP client. When reporting a failure, include the version, tool and filters, but remove tokens, cookies and personal data from logs; do not attach raw account responses.

The numeric property-status and nullable unit-price failures identified in #12 were addressed in [#13](https://github.com/yllibed/TenantCloudClient/pull/13). That fix does not guarantee compatibility with every future change to TenantCloud's internal API.
