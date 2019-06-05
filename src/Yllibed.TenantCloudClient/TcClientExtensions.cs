using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient
{
	public static class TcClientExtensions
	{
		/// <summary>
		/// Helper to get tenants (detailed version) with at least one current lease or in the future.
		/// </summary>
		/// <param name="referenceDate">Reference date to use for check.  Default to "now"</param>
		public static async Task<TcTenantDetails[]> GetActiveTenantsWithActiveOrFutureLease(this ITcClient client, CancellationToken ct, DateTimeOffset? referenceDate = null)
		{
			var dto = referenceDate ?? DateTimeOffset.Now;
			var activeTenants = await client.GetActiveTenants(ct);

			async Task<TcTenantDetails> CheckTenant(TcTenant tenant)
			{
				var tenantDetails = await client.GetTenantDetails(ct, tenant.Id);
				return tenantDetails.HasAnyActiveOrFutureLease(dto) ? tenantDetails : null;
			}

			var tasks = activeTenants.Select(CheckTenant);

			var results = await Task.WhenAll(tasks);

			return results.Where(td => td != null).ToArray();
		}
	}
}