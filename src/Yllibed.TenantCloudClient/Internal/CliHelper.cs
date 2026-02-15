using System.Diagnostics;

namespace Yllibed.TenantCloudClient.Internal;

/// <summary>
/// Async helper for running CLI processes with optional stdin, capturing stdout/stderr.
/// </summary>
internal static class CliHelper
{
	public static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
		string fileName, string arguments, string? stdinData, CancellationToken ct)
	{
		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = stdinData is not null,
				UseShellExecute = false,
				CreateNoWindow = true,
			},
		};

		process.Start();

		if (stdinData is not null)
		{
			await process.StandardInput.WriteAsync(stdinData).ConfigureAwait(false);
			process.StandardInput.Close();
		}

		var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
		var stderrTask = process.StandardError.ReadToEndAsync(ct);

		await process.WaitForExitAsync(ct).ConfigureAwait(false);

		var stdout = await stdoutTask.ConfigureAwait(false);
		var stderr = await stderrTask.ConfigureAwait(false);

		return (process.ExitCode, stdout, stderr);
	}
}
