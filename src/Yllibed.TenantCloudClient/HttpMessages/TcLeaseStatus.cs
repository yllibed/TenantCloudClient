namespace Yllibed.TenantCloudClient.HttpMessages
{
	public enum TcLeaseStatus : byte
	{
		Active = 0,
		Archived = 11,
		Ended = 4,
		Expired = 2,
		ExpiresIn = 13,
		Future = 1,
		InsurancePending = 12,
		NotActive = 10,
		Pending = 9
	}
}