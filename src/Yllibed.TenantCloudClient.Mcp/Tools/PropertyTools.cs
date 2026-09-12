using System.Text.Json.Nodes;
using Repl;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

internal sealed class PropertyTools(ITcClient client)
{
	public IReplPageSource<JsonObject> ListProperties(
		IReplPagingContext paging)
	{
		return TenantCloudPages.Create(client.Properties, paging, McpJsonContext.Default.TcProperty);
	}
}
