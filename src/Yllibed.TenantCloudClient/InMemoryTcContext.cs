using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.TenantCloudClient
{
	/// <summary>
	/// Naive TCContext implementation storing the token in memory.
	/// </summary>
	public class InMemoryTcContext : ITcContext
	{
		private string _token;

		public Task<NetworkCredential> GetCredentials(CancellationToken ct)
		{
			var credentials = new NetworkCredential("hass@le4007.maison", "d7ZZ29&kZSywP3*XD");

			return Task.FromResult(credentials);
		}

		public async Task SetAuthToken(CancellationToken ct, string token)
		{
			_token = token;
		}

		public async Task<string> GetAuthToken(CancellationToken ct)
		{
			return _token;
		}
	}
}
