# Yllibed.TenantCloudClient

Unofficial .NET toolkit for [TenantCloud](https://tenantcloud.com), a rental property management platform. Query your data from a .NET client, the `tc-mcp` command line, an interactive REPL, or an MCP-enabled AI agent.

[![CI](https://github.com/yllibed/TenantCloudClient/actions/workflows/ci.yml/badge.svg)](https://github.com/yllibed/TenantCloudClient/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/dt/Yllibed.TenantCloudClient.svg?label=nuget.org)](https://www.nuget.org/packages/Yllibed.TenantCloudClient)

> **This is not an official TenantCloud product.** TenantCloud does not provide a public API; this library works against their internal endpoints.

## Packages & Binaries

| Component | Type | Description |
|-----------|------|-------------|
| [`Yllibed.TenantCloudClient`](https://www.nuget.org/packages/Yllibed.TenantCloudClient/) | NuGet | Core library: API client, token store abstractions, OS-native secure storage |
| [`Yllibed.TenantCloudClient.Cdp`](https://www.nuget.org/packages/Yllibed.TenantCloudClient.Cdp/) | NuGet | Chromium browser authentication, token refresh and optional interactive sign-in |
| `tc-mcp` | Binary | CLI, interactive REPL and MCP server for AI agents |

The libraries target .NET 8 and .NET 10. Platform-specific `tc-mcp` archives are self-contained ReadyToRun builds; the portable archive requires .NET 10. Browser authentication requires a supported Chromium browser, not Firefox or Safari. See [authentication prerequisites](docs/authentication.md#browser-prerequisites).

The 3.0 line is currently in prerelease. See the [stable release checklist](docs/release-process.md#30-stable-checklist) and [known limitations](docs/client-library.md#known-limitations). Existing applications should read [Migrating to v3](docs/client-library.md#migrating-to-v3).

## Documentation

- **[Client Library](docs/client-library.md)** — Quick start, DI setup, API reference, filters, paginated sources
- **[MCP Server](docs/mcp-server.md)** — Installation, auto-configuration for AI agents, available tools
- **[Authentication](docs/authentication.md)** — CDP flow, SecureTokenStore, FileTokenStore, custom providers
- **[Release Process](docs/release-process.md)** — Stable release criteria, versioning and publication

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

Download the archive for your platform from [GitHub Releases](https://github.com/yllibed/TenantCloudClient/releases), extract **all** its contents and put the directory on your `PATH`. On macOS/Linux, run `chmod +x tc-mcp` if executable permissions were lost during extraction.

```bash
tc-mcp login
tc-mcp list properties --json --result:page-size=20
tc-mcp
```

The last command opens the REPL. Enter `list`, then `properties`; `..` returns to the parent context. See [pagination](docs/mcp-server.md#pagination-and-migration-from-the-previous-mcp-contract) before aggregating results across pages.

For the portable archive, replace `tc-mcp` with `dotnet /path/to/tc-mcp.dll`.

### MCP server for AI agents

With a platform-specific executable in its permanent location:

```bash
# Choose the client you use
tc-mcp install claude-desktop
tc-mcp install claude-code
```

Restart the client, then ask: *"List my TenantCloud properties"* or *"Who are my tenants?"*

Other MCP clients, and portable installations, use [manual stdio configuration](docs/mcp-server.md#manual-configuration). The server command is `tc-mcp mcp serve`; invoking `tc-mcp` without arguments starts the REPL instead.

## License

MIT
