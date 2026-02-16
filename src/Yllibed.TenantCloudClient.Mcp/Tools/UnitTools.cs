using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

[McpServerToolType]
internal sealed class UnitTools
{
	[McpServerTool(Name = "list_units"), Description("List rental units from TenantCloud. Can filter by property or occupancy status.")]
	public static async Task<string> ListUnits(
		ITcClient client,
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
			return JsonSerializer.Serialize(result, McpJsonContext.Default.ListResultTcUnit);
		}
		catch (TcClientException ex)
		{
			return $"Error: {ex.Message} (HTTP {(int)ex.HttpStatus})";
		}
	}
}
