using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AwesomeAssertions;
using Yllibed.TenantCloudClient.Cdp;

namespace Yllibed.TenantCloudClient.Tests;

[TestClass]
public sealed class Given_CdpBrowserStartup
{
	private string _profile = null!;

	[TestInitialize]
	public void CreateProfile() => _profile = Directory.CreateTempSubdirectory("tc-cdp-test-").FullName;

	[TestCleanup]
	public void DeleteProfile() => Directory.Delete(_profile, recursive: true);

	[TestMethod]
	public async Task When_PortFileArrives_Then_WaitsForWorkingEndpoint()
	{
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		var wait = CdpBrowserDiscovery.WaitForReadyAsync(_profile, () => false, TimeSpan.FromSeconds(5), timeout.Token);
		wait.IsCompleted.Should().BeFalse();

		await File.WriteAllTextAsync(Path.Combine(_profile, "DevToolsActivePort"),
			port.ToString(CultureInfo.InvariantCulture) + "\n/devtools/browser/test\n", timeout.Token);

		using var connection = await listener.AcceptTcpClientAsync(timeout.Token);
		await using var stream = connection.GetStream();
		using var reader = new StreamReader(stream, leaveOpen: true);
		var request = await reader.ReadLineAsync(timeout.Token);
		request.Should().StartWith("GET /json ");
		while (!string.IsNullOrEmpty(await reader.ReadLineAsync(timeout.Token)))
		{
		}

		wait.IsCompleted.Should().BeFalse();
		const string body = """[{"type":"page","webSocketDebuggerUrl":"ws://127.0.0.1/devtools/page/test"}]""";
		var response = Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture,
			$"HTTP/1.1 200 OK\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n\r\n{body}"));
		await stream.WriteAsync(response, timeout.Token);
		(await wait).Should().Be(port);
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("12345")]
	[DataRow("0\n/devtools/browser/test")]
	[DataRow("65536\n/devtools/browser/test")]
	[DataRow("invalid\n/devtools/browser/test")]
	public async Task When_PortFileIsIncompleteOrInvalid_Then_TimesOut(string content)
	{
		await File.WriteAllTextAsync(Path.Combine(_profile, "DevToolsActivePort"), content);
		Func<Task> act = () => CdpBrowserDiscovery.WaitForReadyAsync(
			_profile, () => false, TimeSpan.FromMilliseconds(200), CancellationToken.None);
		await act.Should().ThrowAsync<TimeoutException>();
	}

	[TestMethod]
	public async Task When_BrowserExits_Then_FailsWithoutWaitingForLogin()
	{
		Func<Task> act = () => CdpBrowserDiscovery.WaitForReadyAsync(
			_profile, () => true, TimeSpan.FromSeconds(5), CancellationToken.None);
		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[TestMethod]
	public async Task When_Cancelled_Then_PropagatesCancellation()
	{
		using var cancellation = new CancellationTokenSource();
		Func<Task> act = async () =>
		{
			var wait = CdpBrowserDiscovery.WaitForReadyAsync(
				_profile, () => false, TimeSpan.FromSeconds(5), cancellation.Token);
			await cancellation.CancelAsync().ConfigureAwait(false);
			await wait.ConfigureAwait(false);
		};
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
