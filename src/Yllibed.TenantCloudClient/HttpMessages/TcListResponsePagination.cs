using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcListResponsePagination
	{
		[JsonPropertyName("current_page")]
		public long CurrentPage { get; set; }

		[JsonPropertyName("last_page")]
		public long LastPage { get; set; }

		[JsonPropertyName("per_page")]
		public long PerPage { get; set; }

		[JsonPropertyName("total")]
		public long Total { get; set; }
	}
}
