using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcUserInfo
	{
		public long Id { get; set; }
		public string SubDomain { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public string Address1 { get; set; } = string.Empty;
		public string Address2 { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
		public string State { get; set; } = string.Empty;
		public string Zip { get; set; } = string.Empty;
		public bool IsCompany { get; set; }
		public string Company { get; set; } = string.Empty;
		public string Phone { get; set; } = string.Empty;
		public string Fax { get; set; } = string.Empty;
		public string Lang { get; set; } = string.Empty;
		public string IsVip { get; set; } = string.Empty;
	}
}
