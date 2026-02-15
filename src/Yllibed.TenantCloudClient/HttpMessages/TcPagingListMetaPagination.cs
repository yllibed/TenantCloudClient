using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages;

internal class TcPagingListMetaPagination
{
	[JsonPropertyName("count")]
	public long Count { get; set; }

	[JsonPropertyName("current_page")]
	public long CurrentPage { get; set; }

	[JsonPropertyName("par_page")]
	public long PerPage { get; set; }

	[JsonPropertyName("total")]
	public long Total { get; set; }

	[JsonPropertyName("total_pages")]
	public long TotalPages { get; set; }
}
