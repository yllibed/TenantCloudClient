namespace Yllibed.TenantCloudClient.Cdp;

/// <summary>
/// Reads and writes <see cref="TcTokenSet"/> to a JSON file with atomic writes (temp + rename).
/// </summary>
public sealed class FileTokenStore : ITcTokenStore
{
	private readonly string _filePath;

	public FileTokenStore(string filePath)
	{
		_filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
	}

	public async Task<TcTokenSet?> LoadAsync(CancellationToken ct)
	{
		if (!File.Exists(_filePath))
		{
			return null;
		}

		try
		{
			var bytes = await File.ReadAllBytesAsync(_filePath, ct).ConfigureAwait(false);
			return JsonSerializer.Deserialize(bytes, CdpJsonContext.Default.TcTokenSet);
		}
		catch
		{
			return null;
		}
	}

	public async Task SaveAsync(TcTokenSet tokens, CancellationToken ct)
	{
		var dir = Path.GetDirectoryName(_filePath);
		if (!string.IsNullOrEmpty(dir))
		{
			Directory.CreateDirectory(dir);
		}

		var tempPath = _filePath + ".tmp";
		try
		{
			var bytes = JsonSerializer.SerializeToUtf8Bytes(tokens, CdpJsonContext.Default.TcTokenSet);
			await File.WriteAllBytesAsync(tempPath, bytes, ct).ConfigureAwait(false);
			File.Move(tempPath, _filePath, overwrite: true);
		}
		catch
		{
			// Clean up temp file on failure
			try { File.Delete(tempPath); } catch { /* best effort */ }
			throw;
		}
	}
}
