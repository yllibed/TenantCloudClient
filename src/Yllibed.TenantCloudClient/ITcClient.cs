using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient;

/// <summary>
/// Represents a client to send queries to TC server
/// </summary>
public interface ITcClient
{
	/// <summary>
	/// Get information about current signed-in user
	/// </summary>
	Task<TcUserInfo?> GetUserInfo(CancellationToken ct);

	IPaginatedSource<TcContact> Contacts { get; }

	IPaginatedSource<TcProperty> Properties { get; }

	IPaginatedSource<TcUnit> Units { get; }

	IPaginatedSource<TcTransaction> Transactions { get; }

	IPaginatedSource<TcLease> Leases { get; }
}
