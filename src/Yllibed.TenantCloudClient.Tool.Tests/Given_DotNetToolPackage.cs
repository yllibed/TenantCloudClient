using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using AwesomeAssertions;
using ModelContextProtocol.Client;

namespace Yllibed.TenantCloudClient.Tool.Tests;

[TestClass]
[DoNotParallelize]
public sealed class Given_DotNetToolPackage
{
	private const string PackageId = "Yllibed.TenantCloudClient.Tool";
	private static readonly string[] ExpectedTools =
	[
		"get_user_info",
		"list_contacts",
		"list_leases",
		"list_properties",
		"list_transactions",
		"list_units",
	];

	[TestMethod]
	public async Task When_PackageIsDistributed_Then_InstalledAndDnxLaunchersWork()
	{
		var (packageDirectory, packagePath, version) = GetPackageUnderTest();
		var toolPath = Path.Combine(Path.GetTempPath(), "tenantcloud-tool-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(toolPath);

		try
		{
			AssertMinimumRuntimeVersion(packagePath);

			var dnxArguments = new List<string> { "dnx", PackageId };
			if (version.Contains('-', StringComparison.Ordinal))
			{
				dnxArguments.Add("--prerelease");
			}
			dnxArguments.AddRange(["--yes", "--source", packageDirectory, "--"]);

			// Exercise MCP first so dnx must resolve and launch the package from a cold process.
			await AssertMcpContractAsync("dotnet", [.. dnxArguments, "mcp", "serve"]).ConfigureAwait(false);

			var dnxHelp = await RunAsync("dotnet", [.. dnxArguments, "--help", "--no-logo"]).ConfigureAwait(false);
			dnxHelp.ExitCode.Should().Be(0, dnxHelp.StandardError);
			dnxHelp.StandardOutput.Should().Contain("list ...");

			var install = await RunAsync("dotnet",
			[
				"tool", "install", PackageId,
				"--tool-path", toolPath,
				"--source", packageDirectory,
				"--version", version,
			]).ConfigureAwait(false);
			install.ExitCode.Should().Be(0, install.StandardError);

			var toolExecutable = Path.Combine(toolPath, OperatingSystem.IsWindows() ? "tenantcloud.exe" : "tenantcloud");
			File.Exists(toolExecutable).Should().BeTrue();

			var installedTools = await RunAsync("dotnet", ["tool", "list", "--tool-path", toolPath]).ConfigureAwait(false);
			installedTools.ExitCode.Should().Be(0, installedTools.StandardError);
			installedTools.StandardOutput.Should().Contain(PackageId.ToLowerInvariant())
				.And.Contain(version)
				.And.Contain("tenantcloud");

			var cliHelp = await RunAsync(toolExecutable, ["list", "--help", "--no-logo"]).ConfigureAwait(false);
			cliHelp.ExitCode.Should().Be(0, cliHelp.StandardError);
			cliHelp.StandardOutput.Should().Contain("list properties");

			var repl = await RunAsync(toolExecutable, ["--no-logo"], "exit" + Environment.NewLine).ConfigureAwait(false);
			repl.ExitCode.Should().Be(0, repl.StandardError);
			repl.StandardOutput.Should().Contain(">");

			await AssertInstallDnxOptionAsync(toolExecutable).ConfigureAwait(false);

			await AssertMcpContractAsync(toolExecutable, ["mcp", "serve"]).ConfigureAwait(false);
		}
		finally
		{
			Directory.Delete(toolPath, recursive: true);
		}
	}

	private static async Task AssertInstallDnxOptionAsync(string toolExecutable)
	{
		foreach (var dnxOption in new[] { Array.Empty<string>(), new[] { "--dnx" }, new[] { "--dnx=false" } })
		{
			var installArguments = new[] { "install", "unsupported" }.Concat(dnxOption).Append("--no-logo").ToArray();
			var installer = await RunAsync(toolExecutable, installArguments).ConfigureAwait(false);
			installer.ExitCode.Should().Be(1);
			installer.StandardError.Should().Contain("Unknown install target: unsupported")
				.And.NotContain("Unable to bind parameter");
		}
	}

	private static (string PackageDirectory, string PackagePath, string Version) GetPackageUnderTest()
	{
		var packageDirectory = Environment.GetEnvironmentVariable("TENANTCLOUD_TOOL_PACKAGE_DIRECTORY");
		var version = Environment.GetEnvironmentVariable("TENANTCLOUD_TOOL_PACKAGE_VERSION");
		if (string.IsNullOrWhiteSpace(packageDirectory) || string.IsNullOrWhiteSpace(version))
		{
			Assert.Fail("Set the TenantCloud Tool package directory and version to run distribution tests.");
		}

		packageDirectory = Path.GetFullPath(packageDirectory);
		Directory.Exists(packageDirectory).Should().BeTrue();
		var packagePath = Directory.GetFiles(packageDirectory, $"{PackageId}.*.nupkg").Should().ContainSingle().Subject;
		return (packageDirectory, packagePath, version);
	}

	private static void AssertMinimumRuntimeVersion(string packagePath)
	{
		using var package = ZipFile.OpenRead(packagePath);
		var entry = package.GetEntry("tools/net10.0/any/tenantcloud.runtimeconfig.json");
		entry.Should().NotBeNull();
		using var stream = entry!.Open();
		using var document = JsonDocument.Parse(stream);
		var runtimeVersion = document.RootElement.GetProperty("runtimeOptions")
			.GetProperty("framework")
			.GetProperty("version")
			.GetString();
		runtimeVersion.Should().Be("10.0.0");
	}

	private static async Task AssertMcpContractAsync(string command, string[] arguments)
	{
		var standardError = new List<string>();
		var transport = new StdioClientTransport(new()
		{
			Name = "TenantCloud Tool distribution test",
			Command = command,
			Arguments = arguments,
			ShutdownTimeout = TimeSpan.FromSeconds(5),
			StandardErrorLines = standardError.Add,
		});
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
		var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token).ConfigureAwait(false);
		await using (client.ConfigureAwait(false))
		{
			var tools = await client.ListToolsAsync(cancellationToken: timeout.Token).ConfigureAwait(false);
			tools.Select(tool => tool.Name).Should().BeEquivalentTo(ExpectedTools);

			var resources = await client.ListResourcesAsync(cancellationToken: timeout.Token).ConfigureAwait(false);
			resources.Select(resource => resource.Name).Should().BeEquivalentTo(new[] { "guide" });

			var templates = await client.ListResourceTemplatesAsync(cancellationToken: timeout.Token).ConfigureAwait(false);
			templates.Select(template => template.Name).Should().BeEquivalentTo(
				new[] { "property", "unit", "contact" }, string.Join(Environment.NewLine, standardError));
		}
	}

	private static async Task<ProcessResult> RunAsync(
		string command,
		string[] arguments,
		string? standardInput = null)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = command,
			UseShellExecute = false,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};
		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start '{command}'.");
		var standardOutput = process.StandardOutput.ReadToEndAsync();
		var standardError = process.StandardError.ReadToEndAsync();
		if (standardInput is not null)
		{
			await process.StandardInput.WriteAsync(standardInput).ConfigureAwait(false);
		}
		process.StandardInput.Close();

		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
		try
		{
			await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			process.Kill(entireProcessTree: true);
			throw new TimeoutException($"Command '{command}' did not exit within 90 seconds.");
		}

		return new(process.ExitCode,
			await standardOutput.ConfigureAwait(false),
			await standardError.ConfigureAwait(false));
	}

	private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
