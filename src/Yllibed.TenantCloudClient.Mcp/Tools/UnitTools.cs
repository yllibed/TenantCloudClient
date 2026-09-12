using System.ComponentModel;
using System.Text.Json.Nodes;
using Repl;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

internal sealed class UnitTools(ITcClient client, EntityCache cache)
{
	public IReplPageSource<JsonObject> ListUnits(
		IReplPagingContext paging,
		[Description("Filter by property ID")] long? propertyId = null,
		[Description("Filter by occupancy: occupied, vacant")] string? occupancy = null)
	{
		var source = client.Units;

		if (propertyId.HasValue)
		{
			source = source.ForProperty(propertyId.Value);
		}

		source = occupancy?.ToLowerInvariant() switch
		{
			"occupied" => source.OnlyOccuped(),
			"vacant" => source.OnlyVacant(),
			null => source,
			_ => source,
		};

		return TenantCloudPages.Create(source, paging, McpJsonContext.Default.TcUnit, cache);
	}
}
