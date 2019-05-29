using Newtonsoft.Json;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcTenant
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		public string Name { get; set; }
	}
}