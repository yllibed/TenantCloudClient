using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages;

internal class TcPagingListMeta
{
	[JsonPropertyName("pagination")]
	public TcPagingListMetaPagination? Pagination { get; set; }

	[JsonPropertyName("units_count")]
	public long UnitsCount { get; set; }
}
