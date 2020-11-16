using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.TenantCloudClient.Tests
{
	public class InMemoryTcContext : ITcContext
	{
		private readonly string _username;
		private readonly string _password;

		private string _token;

		public InMemoryTcContext(string username, string password)
		{
			_username = username;
			_password = password;
		}

		Task<NetworkCredential> ITcContext.GetCredentials(CancellationToken ct)
		{
			var credentials = new NetworkCredential(_username, _password);

			return Task.FromResult(credentials);
		}

		async Task ITcContext.SetAuthToken(CancellationToken ct, string token)
		{
			_token = token;
		}

		async Task<string> ITcContext.GetAuthToken(CancellationToken ct)
		{
			return _token;
		}
	}
}
