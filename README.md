# Yllibed's TenantCloud API Client Library
This is an unofficial _dotnet_ API client library to connect to [TenantCloud](https://tenantcloud.com),
a cheap / free online Rental Accounting and Management system.

[![Build Status](https://dev.azure.com/yllibed/TenantCloudClient/_apis/build/status/yllibed.TenantCloudClient?branchName=master)](https://dev.azure.com/yllibed/TenantCloudClient/_build/latest?definitionId=1&branchName=master) [![Nuget](https://img.shields.io/nuget/dt/Yllibed.TenantCloudClient.svg?label=nuget.org)](https://www.nuget.org/packages/Yllibed.TenantCloudClient)

> Note: the goal of this API right now is to query the TenantCloud system.  There's no way to make updates to data yet.

## Quickstart

1. Add a reference to the [`Yllibed.TenantCloudClient`](https://www.nuget.org/packages/Yllibed.TenantCloudClient/) nuget package in the project
2. Create a class to handle credentials:
   ``` csharp
       internal class TenantCloudContext : Yllibed.TenantCloudClient.ITcContext
       {
           private string _token;

           public async Task<NetworkCredential> GetCredentials(CancellationToken ct)
           {
               // Implement this method to return credentials for login.
               // It's async, so you can fetch it from a file or an external system.
               return new NetworkCredential("username@domain.tld", "password");

               // Note: please, don't hard-code it in your code!
           }

           public async Task SetAuthToken(CancellationToken ct, string token)
           {
               // Implement this method to persist the authentication token
               // somewhere.
               _token = token;

               // Note: you treat the token as sensitive data like credentials.
           }

           public async Task<string> GetAuthToken(CancellationToken ct)
           {
               // Implement this to retrieve the token from your peristed state.
               return _token;

               // Note: will be called each time a token is required, you may want to cache it.
           }
       }
   ```
3. Make a call:
   ``` csharp
   public async RefreshTenants(CancellationToken ct, TenantCloudContext tcContext)
   {
       // tcContext is an instance of the class created at the previous step
       var client = new TcClient(tcContext);
    var activeTenants = await client.Tenants.GetAll(ct);
   
       // do something fun with activeTenants here...
   }
   ```

## Features

* Coded using `DOTNETSTANDARD2.1`, it means it works on:
  * Dotnet Core 3.0+
  * Xamarin (iOS 12.16+ & Android 10+)
  * Most other _Mono_ 6.4+ environment, except _WebAssembly_ like _Blazor_ or [Uno.BootStrapper](https://github.com/nventive/Uno.Wasm.Bootstrap) because they need a custom http handler - open
  an issue if you need support for those.
* Will automatically renew the authentication token.
* Absolutely no external dependency (except `System.Text.Json` but it's part of the framework)
* No enforced patterns: your code is responsible for the token persistence and the security of credentials.
* Compatible with most/all IoC containers.

## API Details

### `ITcClient`:

Implemented by the class `Yllibed.TenantCloudClient.TcClient`.

| Member                    | Type                                | Usage                                                        |
| ------------------------- | ----------------------------------- | ------------------------------------------------------------ |
| `GetUserInfo` (method)    | `Task<TcUserInfo?>`                 | Get information about the current signed-in user.            |
| `Tenants` (property)      | `IPaginatedSource<TcTenantDetails>` | Get information about tenants                                |
| `Properties` (property)   | `IPaginatedSource<TcProperty>`      | Get information about properties                             |
| `Units` (property)        | `IPaginatedSource<TcUnit>`          | Get information about rented units                           |
| `Transactions` (property) | `IPaginatedSource<TcTransaction>`   | Get transactions. Transactions are always in reversed chronologic order. |

### `IPaginatedSource<T>`:

This interface let you get the `T` items from the API. Results are actually fetched when the `.GetAll()` method is used.

* It's possible to specify a `maxResults` to the `.GetAll()` method to limit the number of results. The TenantCloud API will be fetched using their pagination system until the `maxResults` is reached. This means the `.GetAll()` can return more items than speficied, since it won't slice the last page to the number of `maxResults`. Example:

  ``` csharp
  var results = await tcCLient.Transactions
     .ForCategory(TcTransactionCategory.Expense)
     .ForStatus(TcTransactionStatus.Overdue)
     .GetAll(ct, maxResults: 20);
  ```

  

* The `.GetAll()` method is returning the type `ReadOnlySequence<T>`, which is specialized in returning a list with multiple segments (one per fetched page). If you need to process the results using standard LINQ operators, there's a `.AsEnumerable()` extension method available for that. Example:

  ``` csharp
  var results = await tcClient.Tenants.OnlyNoLease().GetAll(ct);
  var nameEmailsAndPhones = results // results is of type ReadOnlySequence<TcTenantDetails>
      .AsEnumerable() // required to use LINQ operators
      .Select(t=>(t.Name, t.ValidEmails, t.ValidPhones))
      .ToArray();
  ```

### `Tenants` (of type `IPaginatedSource<TcTenantDetails>`)

Will return all non-archived tenants.

* `.OnlyMovedIn()` to filter to "moved in" tenants (with at lease one active lease)
* `.OnlyArchived()` to get archived tenants (this one is not filtering the result, but will return archived instead)
* `.OnlyNoLease()` to filter to tenants without active leases (not "moved in")

## `Properties` (of type `IPaginatedSource<TcProperty>`)

Will return all active (non-archived) properties.  No extension methods for this yet.

## `Units` (of type `IPaginatedSource<TcUnit>`)

Will return tenants. Following filters are possible:

* `.OnlyOccuped()` to filter to units with at least one active lease
* `.OnlyVacant()` to filter to vacant units
* `.ForProperty(propertyId)` to filter for a specific property

## `Transactions` (of type `IPaginatedSource<TcTransaction>`)

Will return transactions in reversed chronological order. Following filters are possible:

* `.ForTenant(tenantId)` to filter for a specific tenant
* `.ForProperty(propertyId)` to filter for a specific property
* `.ForUnit(unitId)` to filter for a specific unit
* `.ForStatus(status)` to filter to a specific `TcTransactionStatus`. Possible values:
  * `TcTransactionStatus.Due`
  * `TcTransactionStatus.Paid`
  * `TcTransactionStatus.Partial`
  * `TcTransactionStatus.Pending`
  * `TcTransactionStatus.Void`
  * `TcTransactionStatus.WithBalance`
  * `TcTransactionStatus.Overdue`
  * `TcTransactionStatus.Waive`
* `.ForCategory(category)` to filter to a specific `TcTransactionCategory`. Possible values:
  * `TcTransactionCategory.Income`
  * `TcTransactionCategory.Expense`
  * `TcTransactionCategory.Refund`
  * `TcTransactionCategory.Credits`
  * `TcTransactionCategory.Liability`

Example usages of `.Transactions`:

``` csharp
// Check if a tenant is having any overdue lease
var isHavingOverdue = (await tcClient.Transactions
	    .ForCategory(TcTransactionCategory.Income)
	    .ForStatus(TcTransactionStatus.Overdue)
	    .ForTenant(tenantId)
	    .GetAll(ct, maxResults: 1))
    .AsEnumerable()
    .Any();

// Get total balance of per property
var balancePerProperty = (await tcClient.Transactions
	    .ForCategory(TcTransactionCategory.Income)
	    .ForStatus(TcTransactionStatus.WithBalance)
	    .GetAll(ct))
    .AsEnumerable()
	.Where(t => t.PropertyId != null) // only property-specific income
    .GroupBy(t => (long)t.PropertyId, t => t.Balance) // group them
    .Select(g => (property: g.Key, balance: g.Sum())) // summarize
    .ToArray(); // create final array

```

