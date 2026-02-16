using System.Globalization;

namespace Yllibed.TenantCloudClient.HttpMessages;

internal static class DateFormats
{
	private static readonly string[] ExactFormats =
	[
		"yyyy-MM-dd",
		"M/d/yyyy",
		"MM/dd/yyyy",
		"MM/d/yyyy",
		"M/dd/yyyy",
	];

	public static bool TryParse(string? value, out DateTimeOffset result)
	{
		result = default;

		if (value is null)
		{
			return false;
		}

		// Try exact date-only formats first
		if (DateTimeOffset.TryParseExact(value, ExactFormats, DateTimeFormatInfo.InvariantInfo,
			DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out result))
		{
			return true;
		}

		// Fallback: ISO 8601 with time (e.g. "2026-02-01T23:00:49.000000Z")
		if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
			DateTimeStyles.RoundtripKind, out result))
		{
			return true;
		}

		return false;
	}
}
