namespace Yllibed.TenantCloudClient.HttpMessages;

public enum TcTransactionStatus : byte
{
	Due = 0,
	Paid = 1,
	Partial = 2,
	Pending = 3,
	Void = 9,
	WithBalance,
	Overdue,
	Waive,
}
