using Newtonsoft.Json;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcProperty
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		public string Name { get; set; }

		[JsonProperty("fullAddress")]
		public string Address { get; set; }

		public TcUnit[] Units { get; set; }
	}
}