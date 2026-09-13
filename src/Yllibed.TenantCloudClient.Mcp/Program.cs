using Yllibed.TenantCloudClient.Mcp;

var app = TenantCloudApp.Create();
if (args.Length == 1 && string.Equals(args[0], "mcp", StringComparison.OrdinalIgnoreCase))
{
	args = ["mcp", "serve"];
}

return await app.RunAsync(args).ConfigureAwait(false);
