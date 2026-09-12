# MCP Server (`tc-mcp`)

`tc-mcp` exposes TenantCloud data as a CLI, an interactive REPL, and a [Model Context Protocol](https://modelcontextprotocol.io) server, using Repl 0.11.

## CLI and interactive use

```bash
tc-mcp list properties --json --result:page-size=20
tc-mcp list transactions --status=with_balance --json --result:page-size=20
tc-mcp list --help
tc-mcp
```

With no arguments, the interactive REPL supports contexts: enter `list`, then run
`properties` or `transactions`. Enter `..` to return to the parent context.

## Installation

Download the binary for your platform from [GitHub Releases](https://github.com/yllibed/TenantCloudClient/releases):

| Platform | Asset |
|----------|-------|
| Windows x64 | `tc-mcp-win-x64.zip` |
| Windows ARM64 | `tc-mcp-win-arm64.zip` |
| macOS x64 | `tc-mcp-osx-x64.zip` |
| macOS ARM64 | `tc-mcp-osx-arm64.zip` |
| Linux x64 | `tc-mcp-linux-x64.zip` |
| Linux ARM64 | `tc-mcp-linux-arm64.zip` |
| Portable (.NET 10) | `tc-mcp-any.zip` |

Each zip contains the executable (`tc-mcp.exe` on Windows, `tc-mcp` on macOS/Linux). Platform-specific builds are self-contained (no .NET runtime required). The portable build requires .NET 10 — run with `dotnet tc-mcp.dll mcp serve`.

## Authentication

Before using the MCP server, authenticate with TenantCloud:

```bash
tc-mcp login
```

This opens a browser window for you to sign in. Tokens are stored in the OS secure credential store (DPAPI on Windows, Keychain on macOS, Secret Service on Linux).

To remove stored credentials:

```bash
tc-mcp logout
```

## Auto-configuration

```bash
# For Claude Desktop
tc-mcp install claude-desktop

# For Claude Code
tc-mcp install claude-code
```

### What `install` does

**Claude Desktop** — patches the config JSON at:
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

**Claude Code** — runs `claude mcp add --transport stdio tc-mcp -- <exePath> mcp serve`

## Manual configuration

### Claude Desktop

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "tc-mcp": {
      "command": "/path/to/tc-mcp",
      "args": ["mcp", "serve"]
    }
  }
}
```

### Claude Code

```bash
claude mcp add --transport stdio tc-mcp -- /path/to/tc-mcp mcp serve
```

### Cursor / other MCP clients

Use stdio transport with the `tc-mcp` binary path as the command and `mcp serve` as arguments.
The previous `tc-mcp mcp` invocation remains an alias.

## Available tools

| Tool | Description | Filters |
|------|-------------|---------|
| `get_user_info` | Current signed-in user profile | — |
| `list_contacts` | Tenants, professionals, etc. | `role` |
| `list_properties` | Rental properties | — |
| `list_units` | Rental units | `propertyId`, `occupancy` |
| `list_transactions` | Financial transactions | `tenantId`, `propertyId`, `unitId`, `status`, `category` |
| `list_leases` | Lease agreements | `propertyId`, `unitId`, `status` |

### Pagination and migration from the previous MCP contract

List tools now return `items` and `pageInfo`, replacing `data` and `count`.
Use `_replPageSize` instead of `maxResults`. Continue with `_replCursor` set to
the previous response's `pageInfo.nextCursor`, keeping the same tool and filters.
A null next cursor means the end. `pageInfo.totalCount` describes the filtered
source total, not the number of rows in the current page.

CLI equivalents are `--result:page-size` and `--result:cursor`. Interactive
human output can load subsequent pages through Repl's pager. Fetching every row
with `--result:all` is intentionally rejected; follow the cursor instead.

Pages are fetched from TenantCloud on demand. Continuing inside an API page may
fetch that page again; results are not a snapshot of changing TenantCloud data.
The existing name-resolution cache may also load bounded lookup lists.

Resolved names now appear beside foreign keys as `property_name`, `unit_name`,
`user_client_name`, and `user_payer_name`, instead of a root `references` map.
Missing names can be resolved through the entity resources or paginated list tools.

## Available resources

| URI | Description |
|-----|-------------|
| `tc://guide` | Tool usage guide — entities, fields, filters, and how to resolve names to IDs |
| `tc://property/{id}` | Property details |
| `tc://unit/{id}` | Unit details, including the parent property name |
| `tc://contact/{id}` | Contact details |

## Authentication

On first use, `tc-mcp` will attempt to authenticate via:

1. **Stored token** — loaded from the OS secure credential store (DPAPI / Keychain / Secret Service)
2. **Running browser** — extracts tokens from a Chromium browser tab open on `app.tenantcloud.com`
3. **Interactive login** — launches a browser window for you to sign in

Tokens are cached and refreshed automatically. See [Authentication](authentication.md) for details.

## Example questions to ask your AI agent

- "Who are my tenants?"
- "What properties do I have?"
- "Which units are vacant?"
- "Show me overdue transactions"
- "List active leases for property 12345"
- "What is the total rent balance?"
