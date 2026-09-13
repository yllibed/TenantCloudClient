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

The default `json-human` format displays JSON fields as key/value records and
supports interactive paging. Nested objects and arrays remain compact JSON.
Use `--json` for machine-readable output; MCP output is unchanged. Explicit
`--human` selects Repl's built-in renderer, which currently displays CLR metadata
for JSON objects. Use the default or `--output:json-human` for readable JSON fields.

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

Extract the **entire archive** into a permanent directory and keep its files and subdirectories together. Do not copy just the executable or DLL. Add that directory to your `PATH`, or use its absolute path in commands and MCP configuration.

Platform-specific archives contain `tc-mcp.exe` on Windows or `tc-mcp` on macOS/Linux. They are self-contained ReadyToRun builds with trimming disabled; no .NET runtime installation is required. On macOS/Linux, run `chmod +x tc-mcp` if your extraction tool did not preserve executable permissions.

The portable archive contains `tc-mcp.dll` and its dependencies, without a native executable. It requires .NET 10. Replace `tc-mcp` in CLI examples with `dotnet /path/to/tc-mcp.dll`, including `login` and interactive use. For MCP, use the [portable configuration](#portable-configuration).

## Authentication

Before using the MCP server, authenticate with TenantCloud:

```bash
tc-mcp login
```

This signs in using a Chromium browser and attempts to persist tokens using OS-backed storage. A usable stored session can be reused; otherwise a temporary browser opens for sign-in. If secure storage is unavailable, login can still succeed for the current process but another process will need to authenticate again. Ordinary signed-in browser windows do not expose CDP automatically. See [browser and storage prerequisites](authentication.md#browser-prerequisites).

To remove stored credentials:

```bash
tc-mcp logout
```

## Auto-configuration

Use these commands with a **platform-specific executable**, not the portable DLL. The installer records the process executable path; when launched through `dotnet`, that path would identify `dotnet` without the DLL argument. Portable installations need manual configuration below.

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

Claude Code must be available as `claude` on `PATH`. Automatic Claude Desktop configuration supports Windows and macOS. Restart the client after changing its configuration.

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

### Portable configuration

Use `dotnet` as the command, with the absolute DLL path before `mcp serve`:

```json
{
  "mcpServers": {
    "tc-mcp": {
      "command": "dotnet",
      "args": ["/absolute/path/to/tc-mcp.dll", "mcp", "serve"]
    }
  }
}
```

The client process must be able to find `dotnet`; otherwise specify its absolute path. On Windows, escape backslashes in JSON paths. With Claude Code:

```bash
claude mcp add --transport stdio tc-mcp -- dotnet /absolute/path/to/tc-mcp.dll mcp serve
```

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

## Known limitations

See the shared [known limitations](client-library.md#known-limitations), especially unconfirmed archived-contact filtering and incomplete MCP error diagnostics. Bounded MCP pages do not repair the client library's separate `GetAll(maxResults)` behavior.

Before calculating totals, follow every continuation cursor with the same filters. A single page is not an account-wide report. Name enrichment and entity resources use the same bounded lookup cache. A missing name or entity can appear on a later list page; an entity resource cannot recover a cache miss by itself.

## Example questions to ask your AI agent

- "Who are my tenants?"
- "What properties do I have?"
- "Which units are vacant?"
- "Show me overdue transactions"
- "List active leases for property 12345"
- "What is the total rent balance?"
