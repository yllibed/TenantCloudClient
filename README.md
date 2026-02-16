# Yllibed.TenantCloudClient

Unofficial .NET toolkit for [TenantCloud](https://tenantcloud.com), a rental property management platform. Includes a client library for programmatic access and an MCP server for AI agent integration.

[![CI](https://github.com/yllibed/TenantCloudClient/actions/workflows/ci.yml/badge.svg)](https://github.com/yllibed/TenantCloudClient/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/dt/Yllibed.TenantCloudClient.svg?label=nuget.org)](https://www.nuget.org/packages/Yllibed.TenantCloudClient)

> **This is not an official TenantCloud product.** TenantCloud does not provide a public API; this library works against their internal endpoints.

## Packages & Binaries

| Component | Type | Description |
|-----------|------|-------------|
| [`Yllibed.TenantCloudClient`](https://www.nuget.org/packages/Yllibed.TenantCloudClient/) | NuGet | Core library: API client, token store abstractions, OS-native secure storage |
| [`Yllibed.TenantCloudClient.Cdp`](https://www.nuget.org/packages/Yllibed.TenantCloudClient.Cdp/) | NuGet | Chrome DevTools Protocol token provider (extracts tokens from a running browser) |
| `tc-mcp` | Binary | MCP server for AI agents (Claude Desktop, Claude Code, Cursor, etc.) |

## Documentation

- **[Client Library](docs/client-library.md)** — Quick start, DI setup, API reference, filters, paginated sources
- **[MCP Server](docs/mcp-server.md)** — Installation, auto-configuration for AI agents, available tools
- **[Authentication](docs/authentication.md)** — CDP flow, SecureTokenStore, FileTokenStore, custom providers

## Quick start

### Client library (NuGet)

```csharp
services
    .AddSecureTokenStore()      // ITcTokenStore → OS credential store
    .AddCdpTokenProvider()      // ITcAuthTokenProvider → browser extraction + auto-refresh
    .AddTenantCloudClient();    // ITcClient → TcClient

// Then inject ITcClient wherever you need it
var user = await tc.GetUserInfo(ct);
var contacts = await tc.Contacts.OnlyTenants().GetAll(ct);
```

### MCP server (binary)

Download `tc-mcp` from [GitHub Releases](https://github.com/yllibed/TenantCloudClient/releases), then:

```bash
# Auto-configure for Claude Desktop or Claude Code
tc-mcp install claude-desktop
tc-mcp install claude-code
```

Then ask your AI agent: *"List my TenantCloud properties"* or *"Who are my tenants?"*

## License

MIT
