using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages;

internal class TcJsonApiPagination
{
	[JsonPropertyName("total")]
	public long Total { get; set; }

	[JsonPropertyName("count")]
	public long Count { get; set; }

	[JsonPropertyName("per_page")]
	public long PerPage { get; set; }

	[JsonPropertyName("current_page")]
	public long CurrentPage { get; set; }

	[JsonPropertyName("total_pages")]
	public long TotalPages { get; set; }
}
