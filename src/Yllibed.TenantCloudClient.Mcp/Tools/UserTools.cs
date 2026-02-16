using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

[McpServerToolType]
internal sealed class UserTools
{
	[McpServerTool(Name = "get_user_info"), Description("Get information about the currently signed-in TenantCloud user.")]
	public static async Task<string> GetUserInfo(ITcClient client, CancellationToken ct)
	{
		try
		{
			var user = await client.GetUserInfo(ct).ConfigureAwait(false);

			if (user is null)
			{
				return "No user info available. You may not be authenticated.";
			}

			return JsonSerializer.Serialize(user, McpJsonContext.Default.TcUserInfo);
		}
		catch (TcClientException ex)
		{
			return $"Error: {ex.Message} (HTTP {(int)ex.HttpStatus})";
		}
	}
}
