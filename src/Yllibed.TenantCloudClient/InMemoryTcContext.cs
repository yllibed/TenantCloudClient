using System;
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
		private readonly Func<CancellationToken, Task<NetworkCredential>> _credentials;
		private string? _token;

		public InMemoryTcContext(Func<CancellationToken, Task<NetworkCredential>> asyncCredentialsCallback)
		{
			_credentials = asyncCredentialsCallback;
		}

		public InMemoryTcContext(Func<CancellationToken, Task<(string username, string password)>> asyncCredentialsCallback)
		{
			_credentials = async ct =>
			{
				var (username, password) = await asyncCredentialsCallback(ct);
				return new NetworkCredential(username, password);
			};
		}

		public InMemoryTcContext(Func<NetworkCredential> credentialsCallback)
		{
			_credentials = _ => Task.FromResult(credentialsCallback());
		}

		public InMemoryTcContext(NetworkCredential credentials)
		{
			_credentials = _ => Task.FromResult(credentials);
		}

		public InMemoryTcContext(string username, string password)
		{
			var credentials = new NetworkCredential(username, password);
			_credentials = _ => Task.FromResult(credentials);
		}

		public Task<NetworkCredential> GetCredentials(CancellationToken ct) => _credentials(ct);

		public Task SetAuthToken(CancellationToken ct, string token)
		{
			_token = token;

			return Task.CompletedTask;
		}

		public Task<string?> GetAuthToken(CancellationToken ct)
		{
			return Task.FromResult(_token);
		}
	}
}
