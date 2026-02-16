using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Yllibed.TenantCloudClient;
using Yllibed.TenantCloudClient.Cdp;
using Yllibed.TenantCloudClient.Mcp;
using Yllibed.TenantCloudClient.Mcp.Tools;

if (args.Length > 0 && string.Equals(args[0], "install", StringComparison.OrdinalIgnoreCase))
{
	return InstallCommand.Run(args.AsSpan(1));
}

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
	.WithTools<LeaseTools>();

await builder.Build().RunAsync().ConfigureAwait(false);

return 0;
