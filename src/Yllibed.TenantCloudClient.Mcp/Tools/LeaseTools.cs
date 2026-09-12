using System.ComponentModel;
using System.Text.Json.Nodes;
using Repl;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

internal sealed class LeaseTools(ITcClient client, EntityCache cache)
{
	public IReplPageSource<JsonObject> ListLeases(
		IReplPagingContext paging,
		[Description("Filter by property ID")] long? propertyId = null,
		[Description("Filter by unit ID")] long? unitId = null,
		[Description("Filter by status: active")] string? status = null)
	{
		var source = client.Leases;

		if (propertyId.HasValue)
		{
			source = source.ForProperty(propertyId.Value);
		}

		if (unitId.HasValue)
		{
			source = source.ForUnit(unitId.Value);
		}

		if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
		{
			source = source.OnlyActive();
		}

		return TenantCloudPages.Create(source, paging, McpJsonContext.Default.TcLease, cache);
	}
}
