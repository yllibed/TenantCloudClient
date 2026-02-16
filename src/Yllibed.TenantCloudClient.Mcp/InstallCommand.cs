using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Yllibed.TenantCloudClient.Mcp;

internal static class InstallCommand
{
	public static int Run(ReadOnlySpan<string> args)
	{
		if (args.Length == 0)
		{
			Console.Error.WriteLine("Usage: tc-mcp install <target>");
			Console.Error.WriteLine("  Targets: claude-desktop, claude-code");
			return 1;
		}

		var target = args[0];

		if (string.Equals(target, "claude-desktop", StringComparison.OrdinalIgnoreCase))
		{
			return InstallClaudeDesktop();
		}

		if (string.Equals(target, "claude-code", StringComparison.OrdinalIgnoreCase))
		{
			return InstallClaudeCode();
		}

		Console.Error.WriteLine($"Unknown install target: {target}");
		Console.Error.WriteLine("  Supported targets: claude-desktop, claude-code");
		return 1;
	}

	private static int InstallClaudeDesktop()
	{
		var exePath = Environment.ProcessPath;
		if (string.IsNullOrEmpty(exePath))
		{
			Console.Error.WriteLine("Error: Could not determine the current executable path.");
			return 1;
		}

		var configPath = GetClaudeDesktopConfigPath();
		if (configPath is null)
		{
			Console.Error.WriteLine("Error: Could not determine Claude Desktop config location for this platform.");
			return 1;
		}

		var configDir = Path.GetDirectoryName(configPath)!;
		if (!Directory.Exists(configDir))
		{
			Directory.CreateDirectory(configDir);
		}

		JsonNode root;
		if (File.Exists(configPath))
		{
			var json = File.ReadAllText(configPath);
			root = JsonNode.Parse(json) ?? new JsonObject();
		}
		else
		{
			root = new JsonObject();
		}

		var servers = root["mcpServers"]?.AsObject();
		if (servers is null)
		{
			servers = new JsonObject();
			root["mcpServers"] = servers;
		}

		servers["tc-mcp"] = new JsonObject
		{
			["command"] = exePath,
			["args"] = new JsonArray(),
		};

		var options = new JsonSerializerOptions { WriteIndented = true };
		var output = root.ToJsonString(options);

		// Atomic write: temp file + rename
		var tempPath = configPath + ".tmp";
		File.WriteAllText(tempPath, output);
		File.Move(tempPath, configPath, overwrite: true);

		Console.WriteLine($"Registered tc-mcp in {configPath}");
		Console.WriteLine("Please restart Claude Desktop to pick up the changes.");
		return 0;
	}

	private static string? GetClaudeDesktopConfigPath()
	{
		if (OperatingSystem.IsWindows())
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			return Path.Combine(appData, "Claude", "claude_desktop_config.json");
		}

		if (OperatingSystem.IsMacOS())
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Path.Combine(home, "Library", "Application Support", "Claude", "claude_desktop_config.json");
		}

		// Linux — no standard Claude Desktop config path
		return null;
	}

	private static int InstallClaudeCode()
	{
		var exePath = Environment.ProcessPath;
		if (string.IsNullOrEmpty(exePath))
		{
			Console.Error.WriteLine("Error: Could not determine the current executable path.");
			return 1;
		}

		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = "claude",
				ArgumentList = { "mcp", "add", "--transport", "stdio", "tc-mcp", "--", exePath },
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			using var process = Process.Start(psi);
			if (process is null)
			{
				Console.Error.WriteLine("Error: Failed to start 'claude' CLI. Is Claude Code installed?");
				return 1;
			}

			process.WaitForExit();

			var stdout = process.StandardOutput.ReadToEnd();
			var stderr = process.StandardError.ReadToEnd();

			if (!string.IsNullOrWhiteSpace(stdout))
			{
				Console.WriteLine(stdout);
			}

			if (!string.IsNullOrWhiteSpace(stderr))
			{
				Console.Error.WriteLine(stderr);
			}

			if (process.ExitCode == 0)
			{
				Console.WriteLine("Successfully registered tc-mcp with Claude Code.");
			}
			else
			{
				Console.Error.WriteLine($"claude mcp add exited with code {process.ExitCode}.");
			}

			return process.ExitCode;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error: {ex.Message}");
			Console.Error.WriteLine("Make sure the 'claude' CLI is installed and available on your PATH.");
			return 1;
		}
	}
}
