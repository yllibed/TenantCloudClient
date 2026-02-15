using System.Text.Json;

namespace Yllibed.TenantCloudClient.Cdp;

internal static class JwtHelper
{
	/// <summary>
	/// Decodes the JWT payload (without signature verification) and reads the "exp" claim.
	/// Returns null if the token is malformed.
	/// </summary>
	public static DateTimeOffset? GetExpiry(string? token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return null;
		}

		try
		{
			var parts = token.Split('.');
			if (parts.Length < 2)
			{
				return null;
			}

			var payload = Base64UrlDecode(parts[1]);
			if (payload is null)
			{
				return null;
			}

			using var doc = JsonDocument.Parse(payload);
			if (doc.RootElement.TryGetProperty("exp", out var expElement))
			{
				var exp = expElement.GetDouble();
				return DateTimeOffset.UnixEpoch.AddSeconds(exp);
			}

			return null;
		}
		catch
		{
			return null;
		}
	}

	private static byte[]? Base64UrlDecode(string input)
	{
		try
		{
			var s = input.Replace('-', '+').Replace('_', '/');
			switch (s.Length % 4)
			{
				case 2: s += "=="; break;
				case 3: s += "="; break;
				case 0: break;
				default: return null;
			}

			return Convert.FromBase64String(s);
		}
		catch
		{
			return null;
		}
	}
}
