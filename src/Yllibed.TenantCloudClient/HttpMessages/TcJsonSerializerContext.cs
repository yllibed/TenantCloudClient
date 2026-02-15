using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages;

[JsonSourceGenerationOptions(
	AllowTrailingCommas = true,
	PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TcUserInfoResponse))]
[JsonSerializable(typeof(TcJsonApiResponse<TcContact>))]
[JsonSerializable(typeof(TcJsonApiResponse<TcProperty>))]
[JsonSerializable(typeof(TcJsonApiResponse<TcUnit>))]
[JsonSerializable(typeof(TcJsonApiResponse<TcTransaction>))]
[JsonSerializable(typeof(TcJsonApiResponse<TcLease>))]
[JsonSerializable(typeof(TcErrorResponse))]
internal partial class TcJsonSerializerContext : JsonSerializerContext
{
}
