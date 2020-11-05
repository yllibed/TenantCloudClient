using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	/// <summary>
	/// Response used by /v1 api
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class TcListResponse<T>
	{
		[JsonPropertyName("list")]
		public T[]? Entries { get; set; }

		[JsonPropertyName("pagination")]
		public TcListResponsePagination? Pagination { get; set; }
	}
}
