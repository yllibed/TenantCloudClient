using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Yllibed.TenantCloudClient.Cdp.CdpMessages;

namespace Yllibed.TenantCloudClient.Cdp;

internal sealed class CdpConnection : IAsyncDisposable
{
	private readonly ClientWebSocket _ws = new();
	private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement?>> _pending = new();
	private int _nextId;
	private CancellationTokenSource? _receiveCts;
	private Task? _receiveLoop;

	public async Task ConnectAsync(Uri wsUrl, TimeSpan timeout, CancellationToken ct)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(timeout);

		await _ws.ConnectAsync(wsUrl, cts.Token).ConfigureAwait(false);

		_receiveCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		_receiveLoop = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token), ct);
	}

	public async Task<TResult?> SendCommandAsync<TResult>(
		string method,
		JsonElement? @params,
		JsonTypeInfo<TResult> resultTypeInfo,
		CancellationToken ct)
	{
		var result = await SendRawCommandAsync(method, @params, ct).ConfigureAwait(false);

		if (result is null)
		{
			return default;
		}

		return result.Value.Deserialize(resultTypeInfo);
	}

	private async Task<JsonElement?> SendRawCommandAsync(
		string method, JsonElement? @params, CancellationToken ct)
	{
		var id = Interlocked.Increment(ref _nextId);
		var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
		_pending[id] = tcs;

		try
		{
			var request = new CdpRequest { Id = id, Method = method, Params = @params };
			var bytes = JsonSerializer.SerializeToUtf8Bytes(request, CdpJsonContext.Default.CdpRequest);

			await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);

			using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

			var registration = timeoutCts.Token.Register(() =>
				tcs.TrySetCanceled(timeoutCts.Token));

			try
			{
				return await tcs.Task.ConfigureAwait(false);
			}
			finally
			{
				await registration.DisposeAsync().ConfigureAwait(false);
			}
		}
		finally
		{
			_pending.TryRemove(id, out _);
		}
	}

	private async Task ReceiveLoopAsync(CancellationToken ct)
	{
		var buffer = new byte[64 * 1024];

		try
		{
			while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
			{
				using var ms = new MemoryStream();

				var endOfMessage = await ReadFullMessageAsync(ms, buffer, ct).ConfigureAwait(false);
				if (!endOfMessage)
				{
					return; // Close frame received
				}

				if (ms.Length > 0)
				{
					DispatchResponse(ms.ToArray());
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Normal shutdown
		}
		catch (WebSocketException)
		{
			// Connection lost
		}
		finally
		{
			CancelAllPending();
		}
	}

	private async Task<bool> ReadFullMessageAsync(
		MemoryStream ms, byte[] buffer, CancellationToken ct)
	{
		WebSocketReceiveResult result;
		do
		{
			result = await _ws.ReceiveAsync(buffer, ct).ConfigureAwait(false);
			if (result.MessageType == WebSocketMessageType.Close)
			{
				return false;
			}

			await ms.WriteAsync(buffer.AsMemory(0, result.Count), ct).ConfigureAwait(false);
		}
		while (!result.EndOfMessage);

		return true;
	}

	private void DispatchResponse(byte[] data)
	{
		try
		{
			var response = JsonSerializer.Deserialize(
				data.AsSpan(),
				CdpJsonContext.Default.CdpResponse);

			if (response is not null && _pending.TryRemove(response.Id, out var tcs))
			{
				if (response.Error is not null)
				{
					tcs.TrySetException(new InvalidOperationException(
						$"CDP error {response.Error.Code}: {response.Error.Message}"));
				}
				else
				{
					tcs.TrySetResult(response.Result);
				}
			}
		}
		catch (JsonException)
		{
			// Ignore non-response messages (events, etc.)
		}
	}

	private void CancelAllPending()
	{
		foreach (var kvp in _pending)
		{
			if (_pending.TryRemove(kvp.Key, out var tcs))
			{
				tcs.TrySetCanceled();
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (_receiveCts is not null)
		{
			await _receiveCts.CancelAsync().ConfigureAwait(false);
		}

		if (_receiveLoop is not null)
		{
			try
			{
#pragma warning disable VSTHRD003 // Awaiting our own background loop during disposal is safe
				await _receiveLoop.ConfigureAwait(false);
#pragma warning restore VSTHRD003
			}
			catch (OperationCanceledException)
			{
				// Expected
			}
		}

		if (_ws.State == WebSocketState.Open)
		{
			try
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
				await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cts.Token).ConfigureAwait(false);
			}
			catch
			{
				// Best effort
			}
		}

		_ws.Dispose();
		_receiveCts?.Dispose();
	}
}
