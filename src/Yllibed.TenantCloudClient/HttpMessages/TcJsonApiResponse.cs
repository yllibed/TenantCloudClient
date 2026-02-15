namespace Yllibed.TenantCloudClient.HttpMessages;

internal class TcJsonApiResponse<T>
{
	[JsonPropertyName("data")]
	public TcJsonApiItem<T>[]? Data { get; set; }

	[JsonPropertyName("meta")]
	public TcJsonApiMeta? Meta { get; set; }
}
