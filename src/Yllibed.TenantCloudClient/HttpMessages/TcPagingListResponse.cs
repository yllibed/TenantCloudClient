using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	/// <summary>
	/// Response use by /v2 api
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class TcPagingListResponse<T>
	{
		[JsonPropertyName("data")]
		public T[]? Entries { get; set; }

		[JsonPropertyName("meta")]
		public TcPagingListMeta? Meta { get; set; }
	}

	internal class TcPagingListMeta
	{
		[JsonPropertyName("pagination")]
		public TcPagingListMetaPagination? Pagination { get; set; }

		[JsonPropertyName("units_count")]
		public long UnitsCount { get; set; }

	}

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
}
