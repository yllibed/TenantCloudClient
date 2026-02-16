using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Yllibed.TenantCloudClient;
using Yllibed.TenantCloudClient.Cdp;
using Yllibed.TenantCloudClient.Mcp;
using Yllibed.TenantCloudClient.Mcp.Tools;

var command = args.Length > 0 ? args[0] : null;

if (string.Equals(command, "install", StringComparison.OrdinalIgnoreCase))
{
	return InstallCommand.Run(args.AsSpan(1));
}

if (string.Equals(command, "mcp", StringComparison.OrdinalIgnoreCase))
{
	return await RunMcpServer(args[1..]).ConfigureAwait(false);
}

if (string.Equals(command, "login", StringComparison.OrdinalIgnoreCase))
{
	return await AuthCommands.LoginAsync().ConfigureAwait(false);
}

if (string.Equals(command, "logout", StringComparison.OrdinalIgnoreCase))
{
	return await AuthCommands.LogoutAsync().ConfigureAwait(false);
}

// No command or unknown command — show help
PrintHelp();
return command is null ? 0 : 1;

static void PrintHelp()
{
	var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev";
	Console.WriteLine($"tc-mcp v{version} — TenantCloud MCP server");
	Console.WriteLine();
	Console.WriteLine("Usage: tc-mcp <command>");
	Console.WriteLine();
	Console.WriteLine("Commands:");
	Console.WriteLine("  mcp                       Start the MCP server (stdio transport)");
	Console.WriteLine("  login                     Authenticate and store tokens");
	Console.WriteLine("  logout                    Remove stored tokens");
	Console.WriteLine("  install claude-desktop     Register in Claude Desktop config");
	Console.WriteLine("  install claude-code        Register in Claude Code via CLI");
}

static async Task<int> RunMcpServer(string[] args)
{
	var builder = Host.CreateApplicationBuilder(args);

	builder.Logging.AddConsole(options =>
	{
		// stdout is reserved for MCP JSON-RPC; route all logs to stderr
		options.LogToStandardErrorThreshold = LogLevel.Trace;
	});

	builder.Services.AddSecureTokenStore();

	// Register CdpTokenProvider directly to set init-only properties via object initializer
	builder.Services.AddSingleton<ITcAuthTokenProvider>(sp =>
		new CdpTokenProvider(new CdpTokenProviderOptions
		{
			TokenStore = sp.GetService<ITcTokenStore>(),
			AllowInteractiveLogin = true,
		}));

	builder.Services.AddTenantCloudClient();
	builder.Services.AddSingleton<EntityCache>();

	builder.Services
		.AddMcpServer(o =>
		{
			o.ServerInfo = new()
			{
				Name = "tc-mcp",
				Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "dev",
			};
		})
		.WithStdioServerTransport()
		.WithTools<UserTools>()
		.WithTools<ContactTools>()
		.WithTools<PropertyTools>()
		.WithTools<UnitTools>()
		.WithTools<TransactionTools>()
		.WithTools<LeaseTools>()
		.WithResources<EntityResources>();

	await builder.Build().RunAsync().ConfigureAwait(false);

	return 0;
}
