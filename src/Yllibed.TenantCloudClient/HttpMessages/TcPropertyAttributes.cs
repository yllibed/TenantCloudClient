using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcPropertyAttributes
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("address1")]
		public string Address1 { get; set; } = string.Empty;

		[JsonPropertyName("cityAddress")]
		public string CityAddress { get; set; } = string.Empty;
	}
}
