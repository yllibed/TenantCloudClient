using Newtonsoft.Json;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	internal class TcListResponse<T>
	{
		[JsonProperty("list")]
		internal T[] Entries { get; set; }
	}
}
