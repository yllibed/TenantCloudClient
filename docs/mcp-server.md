# MCP Server (`tc-mcp`)

`tc-mcp` is a [Model Context Protocol](https://modelcontextprotocol.io) server that exposes TenantCloud data to AI agents like Claude Desktop, Claude Code, and Cursor.

## Installation

Download the binary for your platform from [GitHub Releases](https://github.com/yllibed/TenantCloudClient/releases):

| Platform | Binary |
|----------|--------|
| Windows x64 | `tc-mcp-win-x64.exe` |
| macOS x64 | `tc-mcp-osx-x64` |
| macOS ARM | `tc-mcp-osx-arm64` |
| Linux x64 | `tc-mcp-linux-x64` |

The binary is self-contained (no .NET runtime required).

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

**Claude Code** — runs `claude mcp add --transport stdio tc-mcp -- <exePath>`

## Manual configuration

### Claude Desktop

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "tc-mcp": {
      "command": "/path/to/tc-mcp",
      "args": []
    }
  }
}
```

### Claude Code

```bash
claude mcp add --transport stdio tc-mcp -- /path/to/tc-mcp
```

### Cursor / other MCP clients

Use stdio transport with the `tc-mcp` binary path as the command.

## Available tools

| Tool | Description | Filters |
|------|-------------|---------|
| `get_user_info` | Current signed-in user profile | — |
| `list_contacts` | Tenants, professionals, etc. | `role`, `maxResults` |
| `list_properties` | Rental properties | `maxResults` |
| `list_units` | Rental units | `propertyId`, `occupancy`, `maxResults` |
| `list_transactions` | Financial transactions | `tenantId`, `propertyId`, `unitId`, `status`, `category`, `maxResults` |
| `list_leases` | Lease agreements | `propertyId`, `unitId`, `status`, `maxResults` |

## Available resources

| URI | Description |
|-----|-------------|
| `tc://guide` | Tool usage guide — entities, fields, filters, and how to resolve names to IDs |

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
