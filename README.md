# Yllibed's TenantCloud API Client Library
This is an unofficial _dotnet_ API client library to connect to [TenantCloud](https://tenantcloud.com),
a cheap / free online Rental Accounting and Management system.

[![Build Status](https://dev.azure.com/yllibed/TenantCloudClient/_apis/build/status/yllibed.TenantCloudClient?branchName=master)](https://dev.azure.com/yllibed/TenantCloudClient/_build/latest?definitionId=1&branchName=master) [![Nuget](https://img.shields.io/nuget/dt/Yllibed.TenantCloudClient.svg?label=nuget.org)](https://www.nuget.org/packages/Yllibed.TenantCloudClient)

## Quickstart
1. Add a reference to the [`Yllibed.TenantCloudClient`](https://www.nuget.org/packages/Yllibed.TenantCloudClient/) nuget package from your project
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
       var client = new TcClient(tcContext);
       var activeTenants = await client.GetActiveTenants(ct);

       // do something fun with activeTenants here...
   }
   ```

## Features
* Coded using `DOTNETSTANDARD2.0`, it means it works on:
  * Full dotnet framework (4.6.1+)
  * Dotnet Core 2+
  * Xamarin (iOS & Android)
  * Any other _Mono_ environment, except _WebAssembly_ like _Blazor_ or [Uno.BootStrapper](https://github.com/nventive/Uno.Wasm.Bootstrap) because they need a custom http handler - open
  an issue if you need support for those.
* Will automatically renew the authentication token.
* No external dependency other than _Newtownsoft's JSON.NET_.
* No enforced patterns: your code is responsible for the token persistence and the security of credentials.
* Compatible with most/all IoC containers.
