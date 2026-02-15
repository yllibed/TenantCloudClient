using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Yllibed.TenantCloudClient.Internal;

/// <summary>
/// Windows backend using DPAPI (<c>CryptProtectData</c> / <c>CryptUnprotectData</c>)
/// with encrypted files stored in <c>%LOCALAPPDATA%/Yllibed/TenantCloud/</c>.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsDpapiBackend : ISecureStorageBackend
{
	public Task<byte[]?> LoadAsync(string serviceName, string accountKey, CancellationToken ct)
	{
		var filePath = GetFilePath(accountKey);

		if (!File.Exists(filePath))
		{
			return Task.FromResult<byte[]?>(null);
		}

		var encrypted = File.ReadAllBytes(filePath);
		var entropy = GetEntropy(serviceName);

		var encryptedBlob = new DATA_BLOB(encrypted);
		var entropyBlob = new DATA_BLOB(entropy);
		var decryptedBlob = default(DATA_BLOB);

		try
		{
			if (!CryptUnprotectData(
				ref encryptedBlob,
				nint.Zero,
				ref entropyBlob,
				nint.Zero,
				nint.Zero,
				0,
				ref decryptedBlob))
			{
				return Task.FromResult<byte[]?>(null);
			}

			var result = new byte[decryptedBlob.cbData];
			Marshal.Copy(decryptedBlob.pbData, result, 0, decryptedBlob.cbData);
			return Task.FromResult<byte[]?>(result);
		}
		finally
		{
			encryptedBlob.Free();
			entropyBlob.Free();
			decryptedBlob.Free();
		}
	}

	public Task SaveAsync(string serviceName, string accountKey, byte[] data, CancellationToken ct)
	{
		var filePath = GetFilePath(accountKey);

		var dir = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrEmpty(dir))
		{
			Directory.CreateDirectory(dir);
		}

		var entropy = GetEntropy(serviceName);

		var plaintextBlob = new DATA_BLOB(data);
		var entropyBlob = new DATA_BLOB(entropy);
		var encryptedBlob = default(DATA_BLOB);

		try
		{
			if (!CryptProtectData(
				ref plaintextBlob,
				null,
				ref entropyBlob,
				nint.Zero,
				nint.Zero,
				0,
				ref encryptedBlob))
			{
				throw new InvalidOperationException(
					$"CryptProtectData failed (error 0x{Marshal.GetLastPInvokeError():X8}).");
			}

			var encrypted = new byte[encryptedBlob.cbData];
			Marshal.Copy(encryptedBlob.pbData, encrypted, 0, encryptedBlob.cbData);

			// Atomic write: temp + rename
			var tempPath = filePath + ".tmp";
			try
			{
				File.WriteAllBytes(tempPath, encrypted);
				File.Move(tempPath, filePath, overwrite: true);
			}
			catch
			{
				try { File.Delete(tempPath); } catch { /* best effort */ }
				throw;
			}
		}
		finally
		{
			plaintextBlob.Free();
			entropyBlob.Free();
			encryptedBlob.Free();
		}

		return Task.CompletedTask;
	}

	private static string GetFilePath(string accountKey)
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		return Path.Combine(localAppData, "Yllibed", "TenantCloud", $"tokens-{accountKey}.dpapi");
	}

	private static byte[] GetEntropy(string serviceName) =>
		Encoding.UTF8.GetBytes(serviceName);

	#region P/Invoke

	[StructLayout(LayoutKind.Sequential)]
	private struct DATA_BLOB
	{
		public int cbData;
		public nint pbData;

		public DATA_BLOB(byte[] data)
		{
			cbData = data.Length;
			pbData = Marshal.AllocHGlobal(data.Length);
			Marshal.Copy(data, 0, pbData, data.Length);
		}

		public void Free()
		{
			if (pbData != nint.Zero)
			{
				Marshal.FreeHGlobal(pbData);
				pbData = nint.Zero;
			}
		}
	}

	[DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CryptProtectData(
		ref DATA_BLOB pDataIn,
		string? szDataDescr,
		ref DATA_BLOB pOptionalEntropy,
		nint pvReserved,
		nint pPromptStruct,
		int dwFlags,
		ref DATA_BLOB pDataOut);

	[DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CryptUnprotectData(
		ref DATA_BLOB pDataIn,
		nint ppszDataDescr,
		ref DATA_BLOB pOptionalEntropy,
		nint pvReserved,
		nint pPromptStruct,
		int dwFlags,
		ref DATA_BLOB pDataOut);

	#endregion
}
