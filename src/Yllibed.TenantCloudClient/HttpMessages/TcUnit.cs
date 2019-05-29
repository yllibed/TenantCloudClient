using Newtonsoft.Json;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcUnit
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		public string Name { get; set; }
	}

	public class TcUnitDetails
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		public string Name { get; set; }

		[JsonProperty("is_rented")]
		public bool IsRented { get; set; }

		[JsonProperty("price")]
		public decimal Price { get; set; }

		[JsonProperty("tenants")]
		public TcTenant[] Tenants { get; set; }
	}
}