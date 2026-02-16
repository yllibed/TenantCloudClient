using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

[McpServerToolType]
internal sealed class ContactTools
{
	[McpServerTool(Name = "list_contacts"), Description("List contacts (tenants, professionals) from TenantCloud. Use 'role' to filter by type.")]
	public static async Task<CallToolResult> ListContacts(
		ITcClient client,
		[Description("Filter by role: tenant, professional, moved_in, archived")] string role,
		[Description("Maximum number of results to return (default 100)")] int? maxResults,
		CancellationToken ct)
	{
		try
		{
			var source = client.Contacts;

			source = role?.ToLowerInvariant() switch
			{
				"tenant" => source.OnlyTenants(),
				"professional" => source.OnlyProfessionals(),
				"moved_in" => source.OnlyMovedIn(),
				"archived" => source.OnlyArchived(),
				null => source,
				_ => source,
			};

			var data = await source.GetAll(ct, maxResults ?? 100).ConfigureAwait(false);
			var result = new ListResult<TcContact>(data.AsEnumerable().ToArray());
			return ToolResults.Success(JsonSerializer.Serialize(result, McpJsonContext.Default.ListResultTcContact));
		}
		catch (TcClientException ex)
		{
			return ToolResults.Error($"{ex.Message} (HTTP {(int)ex.HttpStatus})");
		}
	}
}
