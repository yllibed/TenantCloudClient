using System.ComponentModel;
using System.Text.Json.Nodes;
using Repl;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

internal sealed class ContactTools(ITcClient client)
{
	public IReplPageSource<JsonObject> ListContacts(
		IReplPagingContext paging,
		[Description("Filter by role: tenant, professional, moved_in, archived")] string? role = null)
	{
		var source = client.Contacts;

		source = role?.ToLowerInvariant() switch
		{
			"tenant" => source.OnlyTenants(),
			"professional" => source.OnlyProfessionals(),
			"moved_in" => source.OnlyMovedIn(),
			"archived" => source.OnlyArchived(),
			null => source,
			_ => source,
		};

		return TenantCloudPages.Create(source, paging, McpJsonContext.Default.TcContact);
	}
}
