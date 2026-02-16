using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

[McpServerToolType]
internal sealed class UnitTools
{
	[McpServerTool(Name = "list_units"), Description("List rental units from TenantCloud. Can filter by property or occupancy status.")]
	public static async Task<CallToolResult> ListUnits(
		ITcClient client,
		EntityCache cache,
		[Description("Filter by property ID")] long? propertyId,
		[Description("Filter by occupancy: occupied, vacant")] string? occupancy,
		[Description("Maximum number of results to return (default 100)")] int? maxResults,
		CancellationToken ct)
	{
		try
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

			var data = await source.GetAll(ct, maxResults ?? 100).ConfigureAwait(false);
			var result = new ListResult<TcUnit>(data.AsEnumerable().ToArray());
			var json = JsonSerializer.Serialize(result, McpJsonContext.Default.ListResultTcUnit);
			json = await EntityEnricher.EnrichAsync(json, cache, ct).ConfigureAwait(false);
			return ToolResults.Success(json);
		}
		catch (TcClientException ex)
		{
			return ToolResults.Error($"{ex.Message} (HTTP {(int)ex.HttpStatus})");
		}
	}
}
