using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages;

[JsonSourceGenerationOptions(
	AllowTrailingCommas = true,
	PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TcUserInfoResponse))]
[JsonSerializable(typeof(TcListResponse<TcTenantDetails>))]
[JsonSerializable(typeof(TcPagingListResponse<TcProperty>))]
[JsonSerializable(typeof(TcListResponse<TcUnit>))]
[JsonSerializable(typeof(TcListResponse<TcTransaction>))]
[JsonSerializable(typeof(TcErrorResponse))]
internal partial class TcJsonSerializerContext : JsonSerializerContext
{
}
