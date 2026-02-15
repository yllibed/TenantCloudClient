using System.Text.Json.Serialization;
using Yllibed.TenantCloudClient.Cdp.CdpMessages;

namespace Yllibed.TenantCloudClient.Cdp;

[JsonSerializable(typeof(CdpTarget[]))]
[JsonSerializable(typeof(CdpRequest))]
[JsonSerializable(typeof(CdpResponse))]
[JsonSerializable(typeof(CdpCookie))]
[JsonSerializable(typeof(CdpGetCookiesResult))]
[JsonSerializable(typeof(CdpGetCookiesParams))]
[JsonSerializable(typeof(CdpEvaluateResult))]
[JsonSerializable(typeof(CdpEvaluateParams))]
[JsonSerializable(typeof(TcRefreshRequest))]
[JsonSerializable(typeof(TcRefreshResponse))]
[JsonSerializable(typeof(TcTokenSet))]
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class CdpJsonContext : JsonSerializerContext;
