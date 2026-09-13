using Repl;

namespace Yllibed.TenantCloudClient.Mcp;

internal sealed class ManagementModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Map("login", async () => Results.Exit(await AuthCommands.LoginAsync().ConfigureAwait(false)))
			.WithDescription("Authenticate and store tokens.").AutomationHidden();
		map.Map("logout", async () => Results.Exit(await AuthCommands.LogoutAsync().ConfigureAwait(false)))
			.WithDescription("Remove stored tokens.").AutomationHidden();
		map.Context("install", install =>
		{
			install.Map("{target}", (string target, bool dnx = false) => Results.Exit(InstallCommand.Run(target, dnx)))
				.WithDescription("Register with claude-desktop or claude-code.").AutomationHidden();
		});
		map.Context("mcp", mcp =>
		{
			mcp.Map("serve", TenantCloudApp.ServeAsync).WithDescription("Start the MCP stdio server.")
				.AsProtocolPassthrough().AutomationHidden();
		});
	}
}
