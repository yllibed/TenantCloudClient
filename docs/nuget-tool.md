# Yllibed.TenantCloudClient.Tool

Unofficial TenantCloud command-line tool, interactive REPL and MCP server.

> This is not an official TenantCloud product. TenantCloud does not provide a
> public API; this tool works against their internal endpoints.

The tool requires the .NET 10 SDK or runtime and a supported Chromium browser
for interactive authentication.

## Install

While version 3.0 is in prerelease:

```bash
dotnet tool install --global Yllibed.TenantCloudClient.Tool --prerelease
```

Update the prerelease with:

```bash
dotnet tool update --global Yllibed.TenantCloudClient.Tool --prerelease
```

After a stable release is available, omit `--prerelease` to stay on stable
versions. Remove a global installation with:

```bash
dotnet tool uninstall --global Yllibed.TenantCloudClient.Tool
```

## Use

```bash
tenantcloud login
tenantcloud list properties --json --result:page-size=20
tenantcloud
tenantcloud mcp serve
```

Running `tenantcloud` without arguments opens the interactive REPL.

With the .NET 10 SDK, `dnx` can run the latest prerelease without keeping an
installation. This intentionally has no version pin:

```bash
dnx Yllibed.TenantCloudClient.Tool --prerelease --yes -- list properties
```

After stable v3 is available, omit `--prerelease` to run the latest stable
version. To configure an MCP client so it resolves the latest Tool on each
start, use `tenantcloud install claude-desktop --dnx` or `tenantcloud install
claude-code --dnx`.

Version 3 renames the command from `tc-mcp` to `tenantcloud` and does not install
a compatibility alias.

See the [complete documentation](https://github.com/yllibed/TenantCloudClient)
for authentication, pagination and MCP client configuration.
