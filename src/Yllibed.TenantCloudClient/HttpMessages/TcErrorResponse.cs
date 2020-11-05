using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcErrorResponse
	{
		[JsonPropertyName("message")]
		public string? Message { get; set; }
	}
}
