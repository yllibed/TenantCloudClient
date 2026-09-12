using System.Text.Json.Serialization;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp;

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TcUserInfo))]
[JsonSerializable(typeof(TcContact))]
[JsonSerializable(typeof(TcProperty))]
[JsonSerializable(typeof(TcUnit))]
[JsonSerializable(typeof(TcTransaction))]
[JsonSerializable(typeof(TcLease))]
internal partial class McpJsonContext : JsonSerializerContext
{
}
