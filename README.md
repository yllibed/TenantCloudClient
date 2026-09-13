# Yllibed.TenantCloudClient

Unofficial .NET toolkit for [TenantCloud](https://tenantcloud.com), a rental property management platform. Query your data from a .NET client, the `tenantcloud` command line, an interactive REPL, or an MCP-enabled AI agent.

[![CI](https://github.com/yllibed/TenantCloudClient/actions/workflows/ci.yml/badge.svg)](https://github.com/yllibed/TenantCloudClient/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/dt/Yllibed.TenantCloudClient.svg?label=nuget.org)](https://www.nuget.org/packages/Yllibed.TenantCloudClient)

> **This is not an official TenantCloud product.** TenantCloud does not provide a public API; this library works against their internal endpoints.

## Packages & Binaries

| Component | Type | Description |
|-----------|------|-------------|
| [`Yllibed.TenantCloudClient`](https://www.nuget.org/packages/Yllibed.TenantCloudClient/) | NuGet | Core library: API client, token store abstractions, OS-native secure storage |
| [`Yllibed.TenantCloudClient.Cdp`](https://www.nuget.org/packages/Yllibed.TenantCloudClient.Cdp/) | NuGet | Chromium browser authentication, token refresh and optional interactive sign-in |
| [`Yllibed.TenantCloudClient.Tool`](https://www.nuget.org/packages/Yllibed.TenantCloudClient.Tool/) | .NET Tool | `tenantcloud` CLI, interactive REPL and MCP server for AI agents |
| `tenantcloud-*` | Release archive | Self-contained and portable builds of the same tool |

The libraries target .NET 8 and .NET 10. The .NET Tool and portable archive require .NET 10. Platform-specific `tenantcloud` archives are self-contained ReadyToRun builds. Browser authentication requires a supported Chromium browser, not Firefox or Safari. See [authentication prerequisites](docs/authentication.md#browser-prerequisites).

The 3.0 line is currently in prerelease. See the [stable release checklist](docs/release-process.md#30-stable-checklist) and [known limitations](docs/client-library.md#known-limitations). Existing applications should read [Migrating to v3](docs/client-library.md#migrating-to-v3).

## Documentation

- **[Client Library](docs/client-library.md)** — Quick start, DI setup, API reference, filters, paginated sources
- **[MCP Server](docs/mcp-server.md)** — Installation, auto-configuration for AI agents, available tools
- **[Authentication](docs/authentication.md)** — CDP flow, SecureTokenStore, FileTokenStore, custom providers
- **[Release Process](docs/release-process.md)** — Stable release criteria, versioning and publication
- **[Rate limiting](docs/client-library.md#rate-limiting)** — Request pacing, HTTP 429 retries, configuration and errors

## Quick start

### Client library (NuGet)

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

Install both NuGet packages for this example. For dependency injection, see the [client guide](docs/client-library.md#with-dependency-injection).

### CLI and interactive REPL

Install the current v3 prerelease as a global .NET Tool:

```bash
dotnet tool install --global Yllibed.TenantCloudClient.Tool --prerelease
```

After a stable release is available, omit `--prerelease`. To always run the latest prerelease once, without keeping an installation, use .NET 10's `dnx`:

```bash
dnx Yllibed.TenantCloudClient.Tool --prerelease --yes -- list properties
```

Then use the same command for the CLI and REPL:

```bash
tenantcloud login
tenantcloud list properties --json --result:page-size=20
tenantcloud
```

The last command opens the REPL. Enter `list`, then `properties`; `..` returns to the parent context. See [pagination](docs/mcp-server.md#pagination-and-migration-from-the-previous-mcp-contract) before aggregating results across pages.

Self-contained and portable ZIPs remain available from [GitHub Releases](https://github.com/yllibed/TenantCloudClient/releases). For the portable archive, replace `tenantcloud` with `dotnet /path/to/tenantcloud.dll`.

### MCP server for AI agents

Configure a durable installation:

```bash
# Choose the client you use
tenantcloud install claude-desktop
tenantcloud install claude-code
```

To make the MCP client resolve the latest Tool version whenever it starts, add `--dnx` to either command.

Restart the client, then ask: *"List my TenantCloud properties"* or *"Who are my tenants?"*

Other MCP clients, and portable installations, use [manual stdio configuration](docs/mcp-server.md#manual-configuration). The server command is `tenantcloud mcp serve`; invoking `tenantcloud` without arguments starts the REPL instead. Version 3 renames the public command from `tc-mcp` to `tenantcloud`; see the [migration notes](docs/mcp-server.md#v3-command-migration).

## License

MIT
