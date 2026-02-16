using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

[McpServerToolType]
internal sealed class PropertyTools
{
	[McpServerTool(Name = "list_properties"), Description("List rental properties from TenantCloud.")]
	public static async Task<CallToolResult> ListProperties(
		ITcClient client,
		[Description("Maximum number of results to return (default 100)")] int? maxResults,
		CancellationToken ct)
	{
		try
		{
			var data = await client.Properties.GetAll(ct, maxResults ?? 100).ConfigureAwait(false);
			var result = new ListResult<TcProperty>(data.AsEnumerable().ToArray());
			return ToolResults.Success(JsonSerializer.Serialize(result, McpJsonContext.Default.ListResultTcProperty));
		}
		catch (TcClientException ex)
		{
			return ToolResults.Error($"{ex.Message} (HTTP {(int)ex.HttpStatus})");
		}
	}
}
