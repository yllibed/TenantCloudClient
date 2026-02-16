using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

[McpServerToolType]
internal sealed class LeaseTools
{
	[McpServerTool(Name = "list_leases"), Description("List leases from TenantCloud. Can filter by property, unit, or status.")]
	public static async Task<CallToolResult> ListLeases(
		ITcClient client,
		EntityCache cache,
		[Description("Filter by property ID")] long? propertyId,
		[Description("Filter by unit ID")] long? unitId,
		[Description("Filter by status: active")] string? status,
		[Description("Maximum number of results to return (default 100)")] int? maxResults,
		CancellationToken ct)
	{
		try
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

			var data = await source.GetAll(ct, maxResults ?? 100).ConfigureAwait(false);
			var result = new ListResult<TcLease>(data.AsEnumerable().ToArray());
			var json = JsonSerializer.Serialize(result, McpJsonContext.Default.ListResultTcLease);
			json = await EntityEnricher.EnrichAsync(json, cache, ct).ConfigureAwait(false);
			return ToolResults.Success(json);
		}
		catch (TcClientException ex)
		{
			return ToolResults.Error($"{ex.Message} (HTTP {(int)ex.HttpStatus})");
		}
	}
}
