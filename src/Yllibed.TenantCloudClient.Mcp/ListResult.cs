namespace Yllibed.TenantCloudClient.Mcp;

/// <summary>
/// Wrapper for list results returned by MCP tools.
/// </summary>
public sealed record ListResult<T>(T[] Data)
{
	public int Count => Data.Length;
}
