using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Repl;
using Yllibed.TenantCloudClient.Mcp;

namespace Yllibed.TenantCloudClient.Tests;

[TestClass]
public sealed class Given_JsonHumanOutputTransformer
{
	private static JsonHumanOutputTransformer Create() => new(new OutputOptions().Transformers["human"]);

	[TestMethod]
	public async Task When_RenderingJsonObject_Then_UsesJsonFieldsWithoutChangingPayload()
	{
		var value = JsonNode.Parse("""{"name":"Montréal","amount":12.50,"active":true,"missing":null,"tags":["rent"],"unit":{"name":"A"}}""")!;
		var before = value.ToJsonString();
		var text = await Create().TransformAsync(value);
		text.Should().Contain("Montréal").And.Contain("12.50").And.Contain("true").And.Contain("null")
			.And.Contain("[\"rent\"]").And.Contain("{\"name\":\"A\"}");
		text.Should().NotContain("Parent").And.NotContain("Root");
		value.ToJsonString().Should().Be(before);
	}

	[TestMethod]
	public async Task When_RenderingJsonElementArray_Then_PreservesDifferentFields()
	{
		using var document = JsonDocument.Parse("""[{"name":"Alpha"},{"other":2},null]""");
		var text = await Create().TransformAsync(document.RootElement);
		text.Should().Contain("Alpha").And.Contain("other").And.Contain("null").And.NotContain("ValueKind");
	}

	[TestMethod]
	[DataRow("{}")]
	[DataRow("[]")]
	[DataRow("null")]
	[DataRow("true")]
	[DataRow("42")]
	public async Task When_RenderingEmptyOrScalarJson_Then_PreservesValue(string json)
	{
		using var document = JsonDocument.Parse(json);
		(await Create().TransformAsync(document.RootElement)).Should().Be(json);
	}

	[TestMethod]
	public async Task When_RenderingPage_Then_PreservesContinuationAndDoesNotFetch()
	{
		var page = new ReplPage<JsonObject>([new() { ["name"] = "Alpha" }], new(null, "2:0:1", 2, 1));
		var text = await Create().TransformAsync(page);
		text.Should().Contain("Alpha").And.Contain("Showing 1 of 2.").And.Contain("--result:cursor 2:0:1");
		page.PageInfo.NextCursor.Should().Be("2:0:1");
		Create().SupportsInteractivePaging.Should().BeTrue();
	}

	[TestMethod]
	public async Task When_RenderingNonJson_Then_UsesExistingHumanFormatter()
	{
		var fallback = new OutputOptions().Transformers["human"];
		var formatter = new JsonHumanOutputTransformer(fallback);
		var error = Results.Error("test", "Example failure");
		(await formatter.TransformAsync(error)).Should().Be(await fallback.TransformAsync(error));
		(await formatter.TransformAsync("hello")).Should().Be("hello");
	}

	[TestMethod]
	public async Task When_JsonContainsTerminalControls_Then_EscapesThem()
	{
		var value = new JsonObject { ["name\u001b[31m"] = "value\u001b[2J\nnext" };
		var text = await Create().TransformAsync(value);
		text.Should().NotContain("\u001b").And.Contain("\\n");
	}

	[TestMethod]
	public async Task When_Cancelled_Then_StopsRendering()
	{
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		Func<Task> act = async () => await Create().TransformAsync(new JsonObject(), cancellation.Token).ConfigureAwait(false);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
