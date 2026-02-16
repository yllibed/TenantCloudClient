using ModelContextProtocol.Protocol;

namespace Yllibed.TenantCloudClient.Mcp;

internal static class ToolResults
{
	public static CallToolResult Success(string text) =>
		new() { Content = [new TextContentBlock { Text = text }] };

	public static CallToolResult Error(string message) =>
		new() { Content = [new TextContentBlock { Text = message }], IsError = true };
}
