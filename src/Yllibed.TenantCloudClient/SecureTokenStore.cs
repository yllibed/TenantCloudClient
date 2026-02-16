namespace Yllibed.TenantCloudClient;

/// <summary>
/// <see cref="ITcTokenStore"/> implementation that stores tokens in the OS-native credential store:
/// DPAPI on Windows, Keychain on macOS, Secret Service on Linux.
/// </summary>
public sealed class SecureTokenStore : ITcTokenStore
{
	private readonly SecureTokenStoreOptions _options;
	private readonly ISecureStorageBackend _backend;
	private readonly SemaphoreSlim _gate = new(1, 1);

	/// <summary>
	/// Creates a new <see cref="SecureTokenStore"/>.
	/// </summary>
	/// <param name="options">Optional configuration for service name and account key.</param>
	/// <exception cref="PlatformNotSupportedException">
	/// Thrown when the current OS is not Windows, macOS, or Linux.
	/// </exception>
	public SecureTokenStore(SecureTokenStoreOptions? options = null)
	{
		_options = options ?? new SecureTokenStoreOptions();
		_backend = CreateBackend();
	}

	/// <summary>
	/// Gets a value indicating whether the current operating system is supported.
	/// </summary>
	public static bool IsSupported =>
		OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

	/// <inheritdoc />
	public async Task<TcTokenSet?> LoadAsync(CancellationToken ct)
	{
		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			var bytes = await _backend.LoadAsync(
				_options.ServiceName, _options.AccountKey, ct).ConfigureAwait(false);

			if (bytes is null || bytes.Length == 0)
			{
				return null;
			}

			return JsonSerializer.Deserialize(bytes, HttpMessages.TcJsonSerializerContext.Default.TcTokenSet);
		}
		catch
		{
			return null;
		}
		finally
		{
			_gate.Release();
		}
	}

	/// <inheritdoc />
	public async Task SaveAsync(TcTokenSet tokens, CancellationToken ct)
	{
		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			var bytes = JsonSerializer.SerializeToUtf8Bytes(
				tokens, HttpMessages.TcJsonSerializerContext.Default.TcTokenSet);

			await _backend.SaveAsync(
				_options.ServiceName, _options.AccountKey, bytes, ct).ConfigureAwait(false);
		}
		finally
		{
			_gate.Release();
		}
	}

	/// <inheritdoc />
	public async Task DeleteAsync(CancellationToken ct)
	{
		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			await _backend.DeleteAsync(
				_options.ServiceName, _options.AccountKey, ct).ConfigureAwait(false);
		}
		finally
		{
			_gate.Release();
		}
	}

	private static ISecureStorageBackend CreateBackend()
	{
		if (OperatingSystem.IsWindows())
		{
			return new WindowsDpapiBackend();
		}

		if (OperatingSystem.IsMacOS())
		{
			return new MacOsKeychainBackend();
		}

		if (OperatingSystem.IsLinux())
		{
			return new LinuxSecretServiceBackend();
		}

		throw new PlatformNotSupportedException(
			"SecureTokenStore is only supported on Windows, macOS, and Linux.");
	}
}
