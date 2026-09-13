namespace Yllibed.TenantCloudClient;

/// <summary>Controls preventive pacing and HTTP 429 retries for one client instance.</summary>
public sealed class TcRateLimitOptions
{
	/// <summary>Enables pacing, serialized reads and automatic retries.</summary>
	public bool Enabled { get; init; } = true;

	/// <summary>Minimum interval between the start of two data requests.</summary>
	public TimeSpan MinRequestInterval { get; init; } = TimeSpan.FromSeconds(1);

	/// <summary>Maximum additional attempts after HTTP 429 responses.</summary>
	public int MaxRetries { get; init; } = 3;

	/// <summary>Total queue and delay budget per read, excluding network and authentication time.</summary>
	public TimeSpan MaxWait { get; init; } = TimeSpan.FromSeconds(30);

	internal static void Validate(TcRateLimitOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		if (options.MinRequestInterval < TimeSpan.Zero || options.MaxRetries < 0 || options.MaxWait < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(options), "Intervals, retry counts and wait budgets must not be negative.");
		}
		// CancellationTokenSource timers use an unsigned millisecond interval.
		if (options.MaxWait > TimeSpan.FromMilliseconds(uint.MaxValue - 1))
		{
			throw new ArgumentOutOfRangeException(nameof(options), "The wait budget exceeds the supported timer interval.");
		}
	}
}
