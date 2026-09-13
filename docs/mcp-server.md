# CLI, REPL and MCP Server (`tenantcloud`)

`tenantcloud` exposes TenantCloud data as a CLI, an interactive REPL, and a
[Model Context Protocol](https://modelcontextprotocol.io) server, using Repl
0.11.

## Installation

The .NET Tool is the recommended distribution. It requires .NET 10. While v3 is
in prerelease, install or update it with:

```bash
dotnet tool install --global Yllibed.TenantCloudClient.Tool --prerelease
dotnet tool update --global Yllibed.TenantCloudClient.Tool --prerelease
```

After a stable release is available, omit `--prerelease` to stay on stable
versions.

### Run the latest version with `dnx`

.NET 10 can resolve and run the Tool without keeping an installation. These
commands intentionally do not pin a version:

```bash
# Latest stable
dnx Yllibed.TenantCloudClient.Tool --yes -- list properties

# Latest prerelease, while v3 is in prerelease
dnx Yllibed.TenantCloudClient.Tool --prerelease --yes -- list properties
```

Use a normal `dotnet tool install` when repeatability matters. Use `dnx` when
the goal is to follow the newest version in the selected stable or prerelease
channel.

### Release archives

Self-contained and portable ZIPs remain available from
[GitHub Releases](https://github.com/yllibed/TenantCloudClient/releases):

| Platform | Asset |
|----------|-------|
| Windows x64 | `tenantcloud-win-x64.zip` |
| Windows ARM64 | `tenantcloud-win-arm64.zip` |
| macOS x64 | `tenantcloud-osx-x64.zip` |
| macOS ARM64 | `tenantcloud-osx-arm64.zip` |
| Linux x64 | `tenantcloud-linux-x64.zip` |
| Linux ARM64 | `tenantcloud-linux-arm64.zip` |
| Portable (.NET 10) | `tenantcloud-any.zip` |

Extract the entire archive into a permanent directory and keep its files and
subdirectories together. Platform-specific archives contain `tenantcloud.exe`
on Windows or `tenantcloud` on macOS/Linux. They are self-contained ReadyToRun
builds with trimming disabled. On macOS/Linux, run `chmod +x tenantcloud` if
executable permissions were lost during extraction.

The portable archive contains `tenantcloud.dll` and its dependencies. It
requires .NET 10. Replace `tenantcloud` in examples with
`dotnet /absolute/path/to/tenantcloud.dll`.

## CLI and interactive use

```bash
tenantcloud list properties --json --result:page-size=20
tenantcloud list transactions --status=with_balance --json --result:page-size=20
tenantcloud list --help
tenantcloud
```

With no arguments, the interactive REPL supports contexts: enter `list`, then
run `properties` or `transactions`. Enter `..` to return to the parent context.

The default `json-human` format displays JSON fields as key/value records and
supports interactive paging. Nested objects and arrays remain compact JSON.
Use `--json` for machine-readable output; MCP output is unchanged. Explicit
`--human` selects Repl's built-in renderer, which currently displays CLR
metadata for JSON objects. Use the default or `--output:json-human` for readable
JSON fields.

## Authentication

Before using the CLI, REPL or MCP server, authenticate with TenantCloud:

```bash
tenantcloud login
```

This signs in using a Chromium browser and attempts to persist tokens using
OS-backed storage. A usable stored session can be reused; otherwise a temporary
browser opens for sign-in. If secure storage is unavailable, login can still
succeed for the current process but another process will need to authenticate
again. Ordinary signed-in browser windows do not expose CDP automatically. See
[browser and storage prerequisites](authentication.md#browser-prerequisites).

To remove stored credentials:

```bash
tenantcloud logout
```

## Automatic MCP configuration

Configure Claude Desktop or Claude Code from an installed Tool or a
platform-specific archive:

```bash
tenantcloud install claude-desktop
tenantcloud install claude-code
```

The default records the current executable, so it follows that installation.
To make the MCP client resolve the latest Tool version whenever it starts, use:

```bash
tenantcloud install claude-desktop --dnx
tenantcloud install claude-code --dnx
```

The generated stable command is:

```bash
dotnet dnx Yllibed.TenantCloudClient.Tool --yes -- mcp serve
```

A prerelease build also includes `--prerelease`. The command does not include a
version pin. The machine running the MCP client must have the .NET 10 SDK and
network access when `dnx` needs to resolve the package.

Claude Desktop configuration is written to:

- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

Claude Code registration runs `claude mcp add`. The `claude` command must be on
`PATH`. Restart the client after changing its configuration.

## Manual configuration

The logical MCP server identifier remains `tc-mcp`; only the executable command
was renamed.

### Installed Tool or platform-specific archive

```json
{
  "mcpServers": {
    "tc-mcp": {
      "command": "tenantcloud",
      "args": ["mcp", "serve"]
    }
  }
}
```

Use the executable's absolute path if it is not on the MCP client's `PATH`.
With Claude Code:

```bash
claude mcp add --transport stdio tc-mcp -- tenantcloud mcp serve
```

### Latest Tool through `dnx`

Use `dotnet` as the command so the configuration works on every supported
platform:

```json
{
  "mcpServers": {
    "tc-mcp": {
      "command": "dotnet",
      "args": ["dnx", "Yllibed.TenantCloudClient.Tool", "--prerelease", "--yes", "--", "mcp", "serve"]
    }
  }
}
```

Remove `--prerelease` after stable v3 is available if the client should follow
stable releases.

### Portable archive

```json
{
  "mcpServers": {
    "tc-mcp": {
      "command": "dotnet",
      "args": ["/absolute/path/to/tenantcloud.dll", "mcp", "serve"]
    }
  }
}
```

The client process must be able to find `dotnet`; otherwise specify its absolute
path. On Windows, escape backslashes in JSON paths. With Claude Code:

```bash
claude mcp add --transport stdio tc-mcp -- dotnet /absolute/path/to/tenantcloud.dll mcp serve
```

## v3 command migration

Version 3 renames the public executable and .NET Tool command. There is no
`tc-mcp` shell alias.

| Before v3 | v3 |
|-----------|----|
| `tc-mcp login` | `tenantcloud login` |
| `tc-mcp` | `tenantcloud` |
| `tc-mcp mcp serve` | `tenantcloud mcp serve` |
| `tc-mcp-<rid>.zip` | `tenantcloud-<rid>.zip` |
| `tc-mcp.dll` | `tenantcloud.dll` |

Scripts, shortcuts and manual MCP configurations must use the new executable
name. The MCP server key `tc-mcp` may remain unchanged. Rerun `tenantcloud
install ...` to replace an automatically generated launcher.

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
Missing names can be resolved through the entity resources or paginated list
tools.

## Available resources

| URI | Description |
|-----|-------------|
| `tc://guide` | Tool usage guide — entities, fields, filters, and how to resolve names to IDs |
| `tc://property/{id}` | Property details |
| `tc://unit/{id}` | Unit details, including the parent property name |
| `tc://contact/{id}` | Contact details |

## Known limitations

See the shared [known limitations](client-library.md#known-limitations),
especially unconfirmed archived-contact filtering and incomplete MCP error
diagnostics. Bounded MCP pages do not repair the client library's separate
`GetAll(maxResults)` behavior.

Before calculating totals, follow every continuation cursor with the same
filters. A single page is not an account-wide report. Name enrichment and entity
resources use the same bounded lookup cache. A missing name or entity can appear
on a later list page; an entity resource cannot recover a cache miss by itself.

## Example questions to ask your AI agent

- "Who are my tenants?"
- "What properties do I have?"
- "Which units are vacant?"
- "Show me overdue transactions"
- "List active leases for property 12345"
- "What is the total rent balance?"
