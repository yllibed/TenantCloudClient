using System.Net;
using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	internal class TcLoginRequest
	{
		[JsonPropertyName("email")]
		public string? Email { get; set; }

		[JsonPropertyName("password")]
		public string? Password { get; set; }

		[JsonPropertyName("persistent")]
		public int IsPersistent { get; set; } = 1;

		public TcLoginRequest(NetworkCredential netCredentials)
		{
			Email = netCredentials.UserName;
			Password = netCredentials.Password;
		}
	}
}
