using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.TenantCloudClient
{
	public interface ITcContext
	{
		/// <summary>
		/// This method will be called by TcClient when it is required to
		/// login again to update the auth token.
		/// </summary>
		/// <remarks>
		/// Properties `UserName` and `Password` are used for authentication.
		/// </remarks>
		Task<NetworkCredential> GetCredentials(CancellationToken ct);

		/// <summary>
		/// This method will be called by TcClient to persist the auth token.
		/// </summary>
		/// <remarks>
		/// If you can, it should be persisted in a durable manner: like on disk.
		/// You should treat this as the same security sensitivity as credentials.
		/// </remarks>
		Task SetAuthToken(CancellationToken ct, string token);

		/// <summary>
		/// This method will be called by TcClient to get the persisted token.
		/// </summary>
		/// <remarks>
		/// TcClient won't cache it and will call this method each time the token
		/// is required. You should have a mechanism to serve it from cached memory.
		/// </remarks>
		Task<string> GetAuthToken(CancellationToken ct);
	}
}
