namespace Yllibed.TenantCloudClient.Internal;

internal sealed class TcRequestScheduler(TcRateLimitOptions options, TimeProvider timeProvider) : IDisposable
{
	private readonly SemaphoreSlim _gate = new(1, 1);
	private long? _lastStart;
	private long _pauseStart;
	private TimeSpan _pause;

	public WaitBudget CreateBudget() => new(options.MaxWait, timeProvider);

	public async Task EnterAsync(WaitBudget budget, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (!options.Enabled || await _gate.WaitAsync(0, ct).ConfigureAwait(false))
		{
			return;
		}
		if (budget.Remaining <= TimeSpan.Zero)
		{
			throw budget.Exhausted();
		}
		using var timeout = new CancellationTokenSource(budget.Remaining, timeProvider);
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
		var start = timeProvider.GetTimestamp();
		try
		{
			await _gate.WaitAsync(linked.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested && timeout.IsCancellationRequested)
		{
			throw budget.Exhausted();
		}
		finally
		{
			budget.Charge(start);
		}
	}

	public async Task WaitForTurnAsync(WaitBudget budget, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (!options.Enabled)
		{
			return;
		}
		if (budget.Remaining < TimeSpan.Zero)
		{
			throw budget.Exhausted();
		}
		var interval = _lastStart is { } start ? options.MinRequestInterval - timeProvider.GetElapsedTime(start) : TimeSpan.Zero;
		var pause = _pause - timeProvider.GetElapsedTime(_pauseStart);
		var delay = interval > pause ? interval : pause;
		if (delay <= TimeSpan.Zero)
		{
			return;
		}
		if (delay > budget.Remaining)
		{
			throw budget.Exhausted();
		}
		var waitStart = timeProvider.GetTimestamp();
		try
		{
			await Task.Delay(delay, timeProvider, ct).ConfigureAwait(false);
		}
		finally
		{
			budget.Charge(waitStart);
		}
		if (budget.Remaining < TimeSpan.Zero)
		{
			throw budget.Exhausted();
		}
	}

	public void MarkStart()
	{
		if (options.Enabled)
		{
			_lastStart = timeProvider.GetTimestamp();
		}
	}

	public void Pause(TimeSpan delay)
	{
		// Keep the cooldown after cancellation or exhaustion of the initiating read.
		if (delay > _pause - timeProvider.GetElapsedTime(_pauseStart))
		{
			_pauseStart = timeProvider.GetTimestamp();
			_pause = delay;
		}
	}

	public void Exit()
	{
		if (options.Enabled)
		{
			_gate.Release();
		}
	}

	public void Dispose() => _gate.Dispose();

	internal sealed class WaitBudget(TimeSpan remaining, TimeProvider timeProvider)
	{
		public TimeSpan Remaining { get; private set; } = remaining;
		public TcRateLimitException? RateLimitFailure { get; set; }
		public void Charge(long start) => Remaining -= timeProvider.GetElapsedTime(start);
		public Exception Exhausted() => (Exception?)RateLimitFailure ?? new TimeoutException("The client rate limiting wait budget was exhausted before a request could be sent.");
	}
}
