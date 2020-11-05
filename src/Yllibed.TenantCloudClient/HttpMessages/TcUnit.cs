using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcUnit
	{
		[JsonPropertyName("id")]
		[JsonConverter(typeof(JsonAutoLongConverter))]
		public long Id { get; set; }

		[JsonPropertyName("property_id")]
		public long PropertyId { get; set; }

		public string Name { get; set; } = string.Empty;

		public string? Description { get; set; }

		public decimal Price { get; set; }

		[JsonPropertyName("is_rented")]
		public bool IsRented { get; set; }

		[JsonPropertyName("pets_allowed")]
		public bool IsPetAllowed { get; set; }

		[JsonPropertyName("is_furnished")]
		public bool IsFurnished { get; set; }

		[JsonPropertyName("is_utilities")]
		public bool IsUtilities { get; set; }
	}
}
