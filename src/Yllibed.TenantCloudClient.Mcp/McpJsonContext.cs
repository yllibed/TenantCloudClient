using System.Text.Json.Serialization;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp;

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TcUserInfo))]
[JsonSerializable(typeof(ListResult<TcContact>))]
[JsonSerializable(typeof(ListResult<TcProperty>))]
[JsonSerializable(typeof(ListResult<TcUnit>))]
[JsonSerializable(typeof(ListResult<TcTransaction>))]
[JsonSerializable(typeof(ListResult<TcLease>))]
internal partial class McpJsonContext : JsonSerializerContext
{
}
