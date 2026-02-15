namespace Yllibed.TenantCloudClient.HttpMessages;

internal class TcJsonApiMeta
{
	[JsonPropertyName("pagination")]
	public TcJsonApiPagination? Pagination { get; set; }
}
