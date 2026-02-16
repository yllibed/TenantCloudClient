using System.Runtime.Versioning;

namespace Yllibed.TenantCloudClient.Internal;

/// <summary>
/// Linux backend using Secret Service via the <c>secret-tool</c> CLI.
/// Data is Base64-encoded before storage for robustness.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxSecretServiceBackend : ISecureStorageBackend
{
	public async Task<byte[]?> LoadAsync(string serviceName, string accountKey, CancellationToken ct)
	{
		var (exitCode, stdout, _) = await CliHelper.RunAsync(
			"secret-tool",
			$"lookup service \"{serviceName}\" account \"{accountKey}\"",
			stdinData: null,
			ct).ConfigureAwait(false);

		if (exitCode != 0 || string.IsNullOrWhiteSpace(stdout))
		{
			return null;
		}

		return Convert.FromBase64String(stdout.Trim());
	}

	public async Task SaveAsync(string serviceName, string accountKey, byte[] data, CancellationToken ct)
	{
		var base64 = Convert.ToBase64String(data);

		var (exitCode, _, stderr) = await CliHelper.RunAsync(
			"secret-tool",
			$"store --label=\"TenantCloud Tokens\" service \"{serviceName}\" account \"{accountKey}\"",
			stdinData: base64,
			ct).ConfigureAwait(false);

		if (exitCode != 0)
		{
			throw new InvalidOperationException(
				$"secret-tool store failed (exit code {exitCode}): {stderr.Trim()}");
		}
	}

	public async Task DeleteAsync(string serviceName, string accountKey, CancellationToken ct)
	{
		await CliHelper.RunAsync(
			"secret-tool",
			$"clear service \"{serviceName}\" account \"{accountKey}\"",
			stdinData: null,
			ct).ConfigureAwait(false);
	}
}
