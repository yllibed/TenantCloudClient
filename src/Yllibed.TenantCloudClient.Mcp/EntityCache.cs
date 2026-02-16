using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp;

/// <summary>
/// Caches properties, units, and contacts for fast ID-to-name resolution.
/// </summary>
internal sealed class EntityCache(ITcClient client)
{
	private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

	private readonly SemaphoreSlim _semaphore = new(1, 1);
	private Dictionary<long, TcProperty>? _properties;
	private Dictionary<long, TcUnit>? _units;
	private Dictionary<long, TcContact>? _contacts;
	private DateTime _loadedAt;

	public async Task<string?> GetPropertyNameAsync(long id, CancellationToken ct)
	{
		await EnsureCacheAsync(ct).ConfigureAwait(false);
		return _properties!.TryGetValue(id, out var p) ? (p.Name ?? p.Address) : null;
	}

	public async Task<string?> GetUnitNameAsync(long id, CancellationToken ct)
	{
		await EnsureCacheAsync(ct).ConfigureAwait(false);
		return _units!.TryGetValue(id, out var u) ? u.Name : null;
	}

	public async Task<string?> GetContactNameAsync(long id, CancellationToken ct)
	{
		await EnsureCacheAsync(ct).ConfigureAwait(false);
		return _contacts!.TryGetValue(id, out var c) ? c.Name : null;
	}

	public async Task<TcProperty?> GetPropertyAsync(long id, CancellationToken ct)
	{
		await EnsureCacheAsync(ct).ConfigureAwait(false);
		return _properties!.GetValueOrDefault(id);
	}

	public async Task<TcUnit?> GetUnitAsync(long id, CancellationToken ct)
	{
		await EnsureCacheAsync(ct).ConfigureAwait(false);
		return _units!.GetValueOrDefault(id);
	}

	public async Task<TcContact?> GetContactAsync(long id, CancellationToken ct)
	{
		await EnsureCacheAsync(ct).ConfigureAwait(false);
		return _contacts!.GetValueOrDefault(id);
	}

	private async Task EnsureCacheAsync(CancellationToken ct)
	{
		if (_properties is not null && DateTime.UtcNow - _loadedAt < CacheTtl)
		{
			return;
		}

		await _semaphore.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			if (_properties is not null && DateTime.UtcNow - _loadedAt < CacheTtl)
			{
				return;
			}

			var properties = await client.Properties.GetAll(ct).ConfigureAwait(false);
			var units = await client.Units.GetAll(ct).ConfigureAwait(false);
			var contacts = await client.Contacts.GetAll(ct).ConfigureAwait(false);

			_properties = properties.AsEnumerable().ToDictionary(p => p.Id);
			_units = units.AsEnumerable().ToDictionary(u => u.Id);
			_contacts = contacts.AsEnumerable().ToDictionary(c => c.Id);
			_loadedAt = DateTime.UtcNow;
		}
		finally
		{
			_semaphore.Release();
		}
	}
}
