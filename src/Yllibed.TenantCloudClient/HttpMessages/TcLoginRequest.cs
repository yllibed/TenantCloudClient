using System.Net;
using Newtonsoft.Json;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	internal class TcLoginRequest
	{
		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("password")]
		public string Password { get; set; }

		[JsonProperty("persistent")]
		public int IsPersistent { get; set; } = 1;

		public TcLoginRequest() { }

		public TcLoginRequest(NetworkCredential netCredentials)
		{
			Email = netCredentials.UserName;
			Password = netCredentials.Password;
		}
	}
}
