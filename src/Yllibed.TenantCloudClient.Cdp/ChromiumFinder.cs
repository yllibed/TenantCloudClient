using System.Diagnostics;

namespace Yllibed.TenantCloudClient.Cdp;

internal static class ChromiumFinder
{
	public static IReadOnlyList<ChromiumBrowser> FindAll()
	{
		if (OperatingSystem.IsWindows())
		{
			return FindOnWindows();
		}

		if (OperatingSystem.IsMacOS())
		{
			return FindOnMacOS();
		}

		if (OperatingSystem.IsLinux())
		{
			return FindOnLinux();
		}

		return [];
	}

	private static IReadOnlyList<ChromiumBrowser> FindOnWindows()
	{
		var defaultBrowser = DetectWindowsDefaultBrowser();
		var candidates = GetWindowsCandidates();
		return BuildOrderedList(candidates, defaultBrowser);
	}

	private static string? DetectWindowsDefaultBrowser()
	{
		try
		{
			var regOutput = RunProcess("reg",
				@"query ""HKCU\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice"" /v ProgId");
			if (regOutput is null)
			{
				return null;
			}

			foreach (var line in regOutput.Split('\n'))
			{
				if (line.Contains("ProgId", StringComparison.OrdinalIgnoreCase))
				{
					var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length >= 3)
					{
						return parts[^1].Trim();
					}
				}
			}
		}
		catch
		{
			// Registry query failed
		}

		return null;
	}

	private static (string Name, string ProgId, string[] Paths)[] GetWindowsCandidates()
	{
		var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		return
		[
			("Edge", "MSEdgeHTM", [
				Path.Combine(programFilesX86, @"Microsoft\Edge\Application\msedge.exe"),
				Path.Combine(programFiles, @"Microsoft\Edge\Application\msedge.exe"),
			]),
			("Chrome", "ChromeHTML", [
				Path.Combine(programFiles, @"Google\Chrome\Application\chrome.exe"),
				Path.Combine(localAppData, @"Google\Chrome\Application\chrome.exe"),
			]),
			("Brave", "BraveHTML", [
				Path.Combine(programFiles, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
				Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\Application\brave.exe"),
			]),
			("Vivaldi", "VivaldiHTM", [
				Path.Combine(localAppData, @"Vivaldi\Application\vivaldi.exe"),
			]),
			("Chromium", "ChromiumHTM", [
				Path.Combine(localAppData, @"Chromium\Application\chrome.exe"),
			]),
		];
	}

	private static IReadOnlyList<ChromiumBrowser> BuildOrderedList(
		(string Name, string ProgId, string[] Paths)[] candidates,
		string? defaultProgId)
	{
		var results = new List<ChromiumBrowser>();

		// Default browser first
		if (defaultProgId is not null)
		{
			foreach (var (name, progId, paths) in candidates)
			{
				if (defaultProgId.Contains(progId, StringComparison.OrdinalIgnoreCase))
				{
					var exe = paths.FirstOrDefault(File.Exists);
					if (exe is not null)
					{
						results.Add(new ChromiumBrowser(name, exe));
					}

					break;
				}
			}
		}

		// Then all others found on disk
		foreach (var (name, _, paths) in candidates)
		{
			if (results.Any(b => string.Equals(b.Name, name, StringComparison.Ordinal)))
			{
				continue;
			}

			var exe = paths.FirstOrDefault(File.Exists);
			if (exe is not null)
			{
				results.Add(new ChromiumBrowser(name, exe));
			}
		}

		return results;
	}

	private static IReadOnlyList<ChromiumBrowser> FindOnMacOS()
	{
		var defaultBundleId = DetectMacOSDefaultBrowser();
		var candidates = GetMacOSCandidates();

		var results = new List<ChromiumBrowser>();

		// Default browser first
		if (defaultBundleId is not null)
		{
			foreach (var (name, bundleId, path) in candidates)
			{
				if (string.Equals(defaultBundleId, bundleId, StringComparison.OrdinalIgnoreCase)
					&& File.Exists(path))
				{
					results.Add(new ChromiumBrowser(name, path));
					break;
				}
			}
		}

		// Then all others
		foreach (var (name, _, path) in candidates)
		{
			if (results.Any(b => string.Equals(b.Name, name, StringComparison.Ordinal)))
			{
				continue;
			}

			if (File.Exists(path))
			{
				results.Add(new ChromiumBrowser(name, path));
			}
		}

		return results;
	}

	private static string? DetectMacOSDefaultBrowser()
	{
		try
		{
			var plistOutput = RunProcess("plutil",
				"-extract LSHandlers json -o - ~/Library/Preferences/com.apple.LaunchServices/com.apple.launchservices.secure.plist");

			if (plistOutput is null)
			{
				return null;
			}

			var idx = plistOutput.IndexOf("\"LSHandlerURLScheme\":\"https\"", StringComparison.OrdinalIgnoreCase);
			if (idx < 0)
			{
				idx = plistOutput.IndexOf("\"LSHandlerURLScheme\" : \"https\"", StringComparison.OrdinalIgnoreCase);
			}

			if (idx < 0)
			{
				return null;
			}

			var regionStart = Math.Max(0, idx - 200);
			var regionLength = Math.Min(400, plistOutput.Length - regionStart);
			var region = plistOutput.Substring(regionStart, regionLength);
			var roleIdx = region.IndexOf("LSHandlerRoleAll", StringComparison.OrdinalIgnoreCase);

			if (roleIdx < 0)
			{
				return null;
			}

			var afterKey = region.Substring(roleIdx);
			var colonIdx = afterKey.IndexOf(':');
			if (colonIdx < 0)
			{
				return null;
			}

			var quoteStart = afterKey.IndexOf('"', colonIdx);
			if (quoteStart < 0)
			{
				return null;
			}

			var quoteEnd = afterKey.IndexOf('"', quoteStart + 1);
			if (quoteEnd <= quoteStart)
			{
				return null;
			}

			return afterKey.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
		}
		catch
		{
			return null;
		}
	}

	private static (string Name, string BundleId, string Path)[] GetMacOSCandidates()
	{
		return
		[
			("Chrome", "com.google.chrome", "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"),
			("Edge", "com.microsoft.edgemac", "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge"),
			("Brave", "com.brave.browser", "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser"),
			("Vivaldi", "com.vivaldi.vivaldi", "/Applications/Vivaldi.app/Contents/MacOS/Vivaldi"),
			("Chromium", "org.chromium.chromium", "/Applications/Chromium.app/Contents/MacOS/Chromium"),
		];
	}

	private static IReadOnlyList<ChromiumBrowser> FindOnLinux()
	{
		var results = new List<ChromiumBrowser>();

		var candidates = new (string Name, string Command)[]
		{
			("Chrome", "google-chrome"),
			("Chromium", "chromium-browser"),
			("Edge", "microsoft-edge-stable"),
			("Brave", "brave-browser"),
		};

		foreach (var (name, command) in candidates)
		{
			var path = RunProcess("which", command)?.Trim();
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				results.Add(new ChromiumBrowser(name, path));
			}
		}

		return results;
	}

	private static string? RunProcess(string fileName, string arguments)
	{
		try
		{
			using var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = fileName,
					Arguments = arguments,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				},
			};

			process.Start();
			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit(5000);

			return process.ExitCode == 0 ? output : null;
		}
		catch
		{
			return null;
		}
	}
}
