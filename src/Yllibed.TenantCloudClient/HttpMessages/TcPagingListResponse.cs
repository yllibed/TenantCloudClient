using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages;

/// <summary>
/// Response used by /v2 api
/// </summary>
/// <typeparam name="T">The type of entries in the list.</typeparam>
internal class TcPagingListResponse<T>
{
	[JsonPropertyName("data")]
	public T[]? Entries { get; set; }

	[JsonPropertyName("meta")]
	public TcPagingListMeta? Meta { get; set; }
}
