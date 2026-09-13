using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Repl;
using Repl.Mcp;
using Yllibed.TenantCloudClient.Cdp;

namespace Yllibed.TenantCloudClient.Mcp;

internal static class TenantCloudApp
{
	public static ReplApp Create(Action<IServiceCollection>? configure = null)
	{
		var app = ReplApp.Create(services =>
		{
			services.AddLogging(logging => logging.AddConsole(options =>
				options.LogToStandardErrorThreshold = LogLevel.Trace));
			services.AddSecureTokenStore();
			services.AddSingleton<ITcAuthTokenProvider>(sp => new CdpTokenProvider(new CdpTokenProviderOptions
			{
				TokenStore = sp.GetRequiredService<ITcTokenStore>(),
				AllowInteractiveLogin = true,
			}));
			services.AddTenantCloudClient();
			services.AddSingleton<EntityCache>();
			configure?.Invoke(services);
		}).UseDefaultInteractive();
		app.Options(options =>
		{
			var output = options.Output;
			var formatter = new JsonHumanOutputTransformer(output.Transformers["human"]);
			output.AddTransformer(formatter.Name, formatter);
			output.DefaultFormat = formatter.Name;
			output.BannerFormats.Add(formatter.Name);
		});

		app.MapModule<TenantCloudModule>();
		app.MapModule<ManagementModule>();
		return app;
	}

	internal static McpServerOptions BuildMcpOptions(ICoreReplApp app, IServiceProvider services)
	{
		var options = app.BuildMcpServerOptions(o =>
		{
			o.ServerName = "tc-mcp";
			o.ServerVersion = typeof(TenantCloudApp).Assembly.GetName().Version?.ToString() ?? "dev";
			o.AutoPromoteReadOnlyToResources = false;
		}, services);
		// Repl 0.11 emits null paging metadata but declares non-nullable output fields.
		foreach (var tool in options.ToolCollection!)
		{
			if (tool.ProtocolTool.OutputSchema is not { } schema)
			{
				continue;
			}
			var node = JsonNode.Parse(schema.GetRawText())!.AsObject();
			var fields = node["properties"]!["pageInfo"]!["properties"]!;
			fields["cursor"]!["type"] = new JsonArray("string", "null");
			fields["nextCursor"]!["type"] = new JsonArray("string", "null");
			fields["totalCount"]!["type"] = new JsonArray("integer", "null");
			tool.ProtocolTool.OutputSchema = JsonSerializer.SerializeToElement(node);
		}
		options.ResourceCollection = new McpServerResourceCollection
		{
			McpServerResource.Create(SchemaResource.GetSchema, new() { Services = services }),
			McpServerResource.Create(EntityResources.GetProperty, new() { Services = services }),
			McpServerResource.Create(EntityResources.GetUnit, new() { Services = services }),
			McpServerResource.Create(EntityResources.GetContact, new() { Services = services }),
		};
		return options;
	}

	internal static async Task<IExitResult> ServeAsync(ICoreReplApp app, IServiceProvider services, CancellationToken ct)
	{
		var options = BuildMcpOptions(app, services);
		var logging = services.GetRequiredService<ILoggerFactory>();
		var server = McpServer.Create(new StdioServerTransport(options, logging), options, logging, services);
		await using (server.ConfigureAwait(false))
		{
			await server.RunAsync(ct).ConfigureAwait(false);
		}
		return Results.Exit(0);
	}
}
