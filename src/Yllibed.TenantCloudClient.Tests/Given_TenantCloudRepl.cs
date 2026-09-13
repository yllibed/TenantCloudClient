using System.Buffers;
using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Repl;
using Repl.Testing;
using Yllibed.TenantCloudClient.HttpMessages;
using Yllibed.TenantCloudClient.Mcp;

namespace Yllibed.TenantCloudClient.Tests;

[TestClass]
public sealed class Given_TenantCloudRepl
{
	[TestMethod]
	[DataRow(1)]
	[DataRow(3)]
	[DataRow(4)]
	[DataRow(20)]
	public async Task When_Paging_Then_EachItemIsReturnedOnce(int size)
	{
		var client = new FakeClient();
		var source = TenantCloudPages.Create(client.PropertyPages, new PagingContext(), McpJsonContext.Default.TcProperty);
		var ids = new List<long>();
		string? cursor = null;
		do
		{
			var page = await source.FetchAsync(Request(size, cursor));
			page.Items.Count.Should().BeLessThanOrEqualTo(size);
			page.PageInfo.TotalCount.Should().Be(11);
			ids.AddRange(page.Items.Select(item => item["id"]!.GetValue<long>()));
			cursor = page.PageInfo.NextCursor;
		} while (cursor is not null);
		ids.Should().Equal(Enumerable.Range(1, 11).Select(i => (long)i));
		client.PropertyPages.AllCalls.Should().Be(0);
	}

	[TestMethod]
	public async Task When_RequestingOneItem_Then_OnlyFirstApiPageIsFetched()
	{
		var client = new FakeClient();
		var source = TenantCloudPages.Create(client.PropertyPages, new PagingContext(), McpJsonContext.Default.TcProperty);
		var page = await source.FetchAsync(Request(1));
		page.Items.Should().HaveCount(1);
		page.PageInfo.NextCursor.Should().NotBeNull();
		client.PropertyPages.PageCalls.Should().Equal(1);
	}

	[TestMethod]
	[DataRow("garbage")]
	[DataRow("0:0:0")]
	[DataRow("1:-1:0")]
	[DataRow("1:2:1")]
	[DataRow("1:99:99")]
	public async Task When_CursorIsInvalid_Then_Fails(string cursor)
	{
		var source = TenantCloudPages.Create(new FakeClient().Properties, new PagingContext(), McpJsonContext.Default.TcProperty);
		Func<Task> act = async () => await source.FetchAsync(Request(3, cursor)).ConfigureAwait(false);
		await act.Should().ThrowAsync<ArgumentException>();
	}

	[TestMethod]
	public async Task When_AllRequested_Then_DoesNotFetchUnboundedResults()
	{
		var client = new FakeClient();
		var source = TenantCloudPages.Create(client.Properties, new PagingContext(), McpJsonContext.Default.TcProperty);
		Func<Task> act = async () => await source.FetchAsync(Request(3) with { AllRequested = true }).ConfigureAwait(false);
		await act.Should().ThrowAsync<ArgumentException>();
		client.PropertyPages.PageCalls.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Cancelled_Then_DoesNotFetch()
	{
		var client = new FakeClient();
		var source = TenantCloudPages.Create(client.Properties, new PagingContext(), McpJsonContext.Default.TcProperty);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		Func<Task> act = async () => await source.FetchAsync(Request(3), cancellation.Token).ConfigureAwait(false);
		await act.Should().ThrowAsync<OperationCanceledException>();
		client.PropertyPages.PageCalls.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Empty_Then_NoContinuation()
	{
		var source = TenantCloudPages.Create(new FakePages<TcProperty>([]), new PagingContext(), McpJsonContext.Default.TcProperty);
		var page = await source.FetchAsync(Request(3));
		page.Items.Should().BeEmpty();
		page.PageInfo.NextCursor.Should().BeNull();
	}

	[TestMethod]
	public async Task When_UsingCommandGraph_Then_PagingWorksAcrossSessions()
	{
		await using var host = ReplTestHost.Create(() => TenantCloudApp.Create(s => s.AddSingleton<ITcClient>(new FakeClient())));
		await using var first = await host.OpenSessionAsync();
		await using var second = await host.OpenSessionAsync();
		var result = await first.RunCommandAsync("list properties --json --result:page-size=3 --no-logo");
		result.ExitCode.Should().Be(0, result.OutputText);
		var body = result.ReadJson<JsonElement>();
		body.GetProperty("items").GetArrayLength().Should().Be(3);
		var cursor = body.GetProperty("pageInfo").GetProperty("nextCursor").GetString();
		var next = await second.RunCommandAsync($"list properties --json --result:page-size=3 --result:cursor={cursor} --no-logo");
		next.ExitCode.Should().Be(0, next.OutputText);
		next.ReadJson<JsonElement>().GetProperty("items")[0].GetProperty("id").GetInt64().Should().Be(4);
		var help = await first.RunCommandAsync("list --help --no-logo");
		help.ExitCode.Should().Be(0);
		help.OutputText.Should().Contain("properties");
		var invalid = await first.RunCommandAsync("list properties --json --result:cursor=invalid --no-logo");
		invalid.ExitCode.Should().NotBe(0);
	}

	[TestMethod]
	public async Task When_UsingDefaultOutput_Then_JsonFieldsAreReadable()
	{
		await using var host = ReplTestHost.Create(() => TenantCloudApp.Create(s => s.AddSingleton<ITcClient>(new FakeClient())));
		await using var session = await host.OpenSessionAsync();
		var result = await session.RunCommandAsync("list properties --result:page-size=1 --no-logo");
		result.ExitCode.Should().Be(0, result.OutputText);
		result.OutputText.Should().Contain("Property 1").And.Contain("name").And.Contain("--result:cursor");
		result.OutputText.Should().NotContain("Parent").And.NotContain("Root").And.NotContain("Options");
		var user = await session.RunCommandAsync("get user info --no-logo");
		user.ExitCode.Should().Be(0, user.OutputText);
		user.OutputText.Should().Contain("id").And.NotContain("Parent");
	}

	[TestMethod]
	public void When_BuildingMcpGraph_Then_OnlyDataToolsAreExposed()
	{
		var app = TenantCloudApp.Create(s => s.AddSingleton<ITcClient>(new FakeClient()));
		var options = TenantCloudApp.BuildMcpOptions(app.Services.GetRequiredService<ICoreReplApp>(), app.Services);
		options.ToolCollection!.Select(t => t.ProtocolTool.Name).Should().BeEquivalentTo(
			"get_user_info", "list_contacts", "list_properties", "list_units", "list_transactions", "list_leases");
		foreach (var tool in options.ToolCollection!.Where(t => t.ProtocolTool.Name.StartsWith("list_", StringComparison.Ordinal)))
		{
			var properties = tool.ProtocolTool.InputSchema.GetProperty("properties");
			properties.TryGetProperty("_replCursor", out _).Should().BeTrue();
			properties.TryGetProperty("_replPageSize", out _).Should().BeTrue();
			properties.TryGetProperty("maxResults", out _).Should().BeFalse();
			properties.TryGetProperty("client", out _).Should().BeFalse();
			properties.TryGetProperty("cache", out _).Should().BeFalse();
			var pageInfo = tool.ProtocolTool.OutputSchema!.Value.GetProperty("properties")
				.GetProperty("pageInfo").GetProperty("properties");
			foreach (var field in new[] { "cursor", "nextCursor", "totalCount" })
			{
				pageInfo.GetProperty(field).GetProperty("type").EnumerateArray()
					.Select(t => t.GetString()).Should().Contain("null");
			}
		}
		options.ResourceCollection.Should().HaveCount(4);
	}

	[TestMethod]
	public void When_CreatingStableDnxLauncher_Then_UsesLatestStablePackage()
	{
		var launch = InstallCommand.CreateDnxLaunchCommand("3.0.61+abcdef");

		launch.Command.Should().Be("dotnet");
		launch.Arguments.Should().ContainInOrder(
			"dnx", "Yllibed.TenantCloudClient.Tool", "--yes", "--", "mcp", "serve");
		launch.Arguments.Should().NotContain("--prerelease");
	}

	[TestMethod]
	public void When_CreatingPrereleaseDnxLauncher_Then_UsesLatestPrereleasePackage()
	{
		var launch = InstallCommand.CreateDnxLaunchCommand("3.0.61-dev+abcdef");

		launch.Command.Should().Be("dotnet");
		launch.Arguments.Should().ContainInOrder(
			"dnx", "Yllibed.TenantCloudClient.Tool", "--prerelease", "--yes", "--", "mcp", "serve");
	}

	[TestMethod]
	public void When_ReadingGuide_Then_EntityFieldsMatchToolJson()
	{
		var guide = SchemaResource.GetSchema();
		foreach (var field in new[]
		{
			"property_status", "property_id", "is_rented", "pets_allowed", "is_furnished", "is_utilities",
			"unit_id", "user_payer_id", "date", "paid_at", "created_at", "is_recurring",
			"user_client_id", "rent_from", "rent_to", "move_out_date", "lease_status",
		})
		{
			guide.Should().Contain($"`{field}`");
		}

		var entities = guide[..guide.IndexOf("## Tool Filters", StringComparison.Ordinal)];
		entities.Should().NotContain("`propertyId`")
			.And.NotContain("`payerId`")
			.And.NotContain("`tenantId`");
		guide.Split('\n').Single(line => line.Contains("`price`", StringComparison.Ordinal))
			.Should().StartWith("- `price`");
	}

	private static ReplPageRequest Request(int size, string? cursor = null) => new(size, cursor, null, false, ReplResultSurface.Programmatic);

	private sealed class PagingContext : IReplPagingContext
	{
		public int? VisibleRowCapacityHint => null;
		public int SuggestedPageSize => 100;
		public int MaxPageSize => 1000;
		public string? Cursor => null;
		public bool AllRequested => false;
		public ReplResultSurface Surface => ReplResultSurface.Programmatic;
	}

	private sealed class FakeClient : ITcClient
	{
		public FakePages<TcProperty> PropertyPages { get; } = new(Enumerable.Range(1, 11)
			.Select(i => new TcProperty { Id = i, Name = "Property " + i.ToString(CultureInfo.InvariantCulture) }).ToArray());
		public IPaginatedSource<TcProperty> Properties => PropertyPages;
		public IPaginatedSource<TcContact> Contacts { get; } = new FakePages<TcContact>([]);
		public IPaginatedSource<TcUnit> Units { get; } = new FakePages<TcUnit>([]);
		public IPaginatedSource<TcTransaction> Transactions { get; } = new FakePages<TcTransaction>([]);
		public IPaginatedSource<TcLease> Leases { get; } = new FakePages<TcLease>([]);
		public Task<TcUserInfo?> GetUserInfo(CancellationToken ct) => Task.FromResult<TcUserInfo?>(new() { Id = 1 });
	}

	private sealed class FakePages<T>(T[] items) : IPaginatedSource<T>
	{
		public List<long> PageCalls { get; } = [];
		public int AllCalls { get; private set; }
		public Task<(ReadOnlyMemory<T> entries, long pageNo, long totalEntries)> GetPage(CancellationToken ct, long pageNo = 1)
		{
			ct.ThrowIfCancellationRequested();
			PageCalls.Add(pageNo);
			var page = items.Skip(checked((int)(pageNo - 1) * 4)).Take(4).ToArray();
			return Task.FromResult<(ReadOnlyMemory<T>, long, long)>((page, pageNo, items.Length));
		}
		public Task<ReadOnlySequence<T>> GetAll(CancellationToken ct, long maxResults = 300)
		{
			AllCalls++;
			return Task.FromResult(new ReadOnlySequence<T>(items));
		}
	}
}
