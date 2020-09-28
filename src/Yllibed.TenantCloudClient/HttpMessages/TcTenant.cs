using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcTenant
	{
		[JsonPropertyName("id")]
		[JsonConverter(typeof(JsonAutoLongConverter))]
		public long Id { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;
	}
}
