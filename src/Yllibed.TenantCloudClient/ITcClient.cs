using System.Threading;
using System.Threading.Tasks;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient
{
	/// <summary>
	/// Represents a client to send queries to TC server
	/// </summary>
	public interface ITcClient
	{
		/// <summary>
		/// Get all non-archived tenants.
		/// </summary>
		Task<TcTenant[]> GetActiveTenants(CancellationToken ct);

		/// <summary>
		/// Get details about a particular tenant.
		/// </summary>
		Task<TcTenantDetails> GetTenantDetails(CancellationToken ct, long tenantId);

		/// <summary>
		/// Get all properties.
		/// </summary>
		Task<TcProperty[]> GetProperties(CancellationToken ct);

		/// <summary>
		/// Get details about a property.
		/// </summary>
		Task<TcUnitDetails[]> GetUnitDetails(CancellationToken ct, long propertyId);
	}
}
