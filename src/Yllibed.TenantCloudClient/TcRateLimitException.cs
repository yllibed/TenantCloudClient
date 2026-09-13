using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Yllibed.TenantCloudClient;

/// <summary>A TenantCloud HTTP 429 response that could not be retried within the configured limits.</summary>
public sealed class TcRateLimitException : TcClientException
{
	/// <summary>The server's requested delay, or null if no valid Retry-After was supplied.</summary>
	public TimeSpan? RetryAfter { get; }

	/// <summary>The server's announced quota, without an assumed time window.</summary>
	public long? Limit { get; }

	/// <summary>The server's announced remaining quota.</summary>
	public long? Remaining { get; }

	internal TcRateLimitException(HttpResponseMessage response, TimeProvider timeProvider)
		: base(HttpStatusCode.TooManyRequests, "TenantCloud rate limit reached. Retry later or adjust the client's rate limiting options.")
	{
		if (RetryConditionHeaderValue.TryParse(ReadHeader(response.Headers, "Retry-After"), out var retry))
		{
			var delay = retry.Delta ?? (retry.Date!.Value - (response.Headers.Date ?? timeProvider.GetUtcNow()));
			RetryAfter = delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
		}
		Limit = ReadQuota(response.Headers, "X-Ratelimit-Limit");
		Remaining = ReadQuota(response.Headers, "X-Ratelimit-Remaining");
	}

	private static long? ReadQuota(HttpResponseHeaders headers, string name) =>
		long.TryParse(ReadHeader(headers, name), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;

	private static string? ReadHeader(HttpResponseHeaders headers, string name)
	{
		if (!headers.TryGetValues(name, out var values))
		{
			return null;
		}
		using var enumerator = values.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			return null;
		}
		var value = enumerator.Current;
		return enumerator.MoveNext() ? null : value;
	}
}
