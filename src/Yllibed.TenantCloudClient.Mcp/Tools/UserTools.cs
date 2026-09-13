using System.Text.Json;
using Repl;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

internal sealed class UserTools(ITcClient client)
{
	public async Task<object> GetUserInfo(CancellationToken ct)
	{
		try
		{
			var user = await client.GetUserInfo(ct).ConfigureAwait(false);

			if (user is null)
			{
				return Results.Error("authentication", "No user info available. You may not be authenticated.");
			}

			return JsonSerializer.SerializeToNode(user, McpJsonContext.Default.TcUserInfo)!;
		}
		catch (TcClientException ex)
		{
			return Results.Error("tenantcloud", $"{ex.Message} (HTTP {(int)ex.HttpStatus})");
		}
	}
}
