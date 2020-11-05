using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
		/// Get information about current signed-in user
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		Task<TcUserInfo?> GetUserInfo(CancellationToken ct);

		IPaginatedSource<TcTenantDetails> Tenants { get; }

		IPaginatedSource<TcProperty> Properties { get; }

		IPaginatedSource<TcUnit> Units { get; }

		IPaginatedSource<TcTransaction> Transactions { get; }
	}
}
