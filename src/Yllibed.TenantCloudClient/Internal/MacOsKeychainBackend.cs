using System.Runtime.Versioning;

namespace Yllibed.TenantCloudClient.Internal;

/// <summary>
/// macOS backend using the Keychain via the <c>security</c> CLI.
/// Data is Base64-encoded before storage for robustness.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacOsKeychainBackend : ISecureStorageBackend
{
	public async Task<byte[]?> LoadAsync(string serviceName, string accountKey, CancellationToken ct)
	{
		var (exitCode, stdout, _) = await CliHelper.RunAsync(
			"security",
			$"find-generic-password -s \"{serviceName}\" -a \"{accountKey}\" -w",
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
			"security",
			$"add-generic-password -s \"{serviceName}\" -a \"{accountKey}\" -w \"{base64}\" -U",
			stdinData: null,
			ct).ConfigureAwait(false);

		if (exitCode != 0)
		{
			throw new InvalidOperationException(
				$"security add-generic-password failed (exit code {exitCode}): {stderr.Trim()}");
		}
	}
}
