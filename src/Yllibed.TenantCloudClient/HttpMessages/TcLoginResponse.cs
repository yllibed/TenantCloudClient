using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcLoginResponse
	{
		[JsonPropertyName("token_type")]
		public string? TokenType { get; set; }

		[JsonPropertyName("expires_in")]
		public ulong? ExpiresIn { get; set; }

		[JsonPropertyName("access_token")]
		public string? AccessToken { get; set; }

		[JsonPropertyName("refresh_token")]
		public string? RefreshToken { get; set; }

		[JsonPropertyName("user_id")]
		public long? UserId { get; set; }
	}
}
