using System.Text.Json.Nodes;

namespace Yllibed.TenantCloudClient.Mcp;

internal static class EntityEnricher
{
	public static async Task EnrichAsync(IReadOnlyList<JsonObject> items, EntityCache cache, CancellationToken ct)
	{
		foreach (var item in items)
		{
			await AddNameAsync(item, "property_id", "property_name", cache.GetPropertyNameAsync, ct).ConfigureAwait(false);
			await AddNameAsync(item, "unit_id", "unit_name", cache.GetUnitNameAsync, ct).ConfigureAwait(false);
			await AddNameAsync(item, "user_client_id", "user_client_name", cache.GetContactNameAsync, ct).ConfigureAwait(false);
			await AddNameAsync(item, "user_payer_id", "user_payer_name", cache.GetContactNameAsync, ct).ConfigureAwait(false);
		}
	}

	private static async Task AddNameAsync(JsonObject item, string idField, string nameField,
		Func<long, CancellationToken, Task<string?>> resolve, CancellationToken ct)
	{
		if (item[idField] is JsonValue value && value.TryGetValue<long>(out var id)
			&& await resolve(id, ct).ConfigureAwait(false) is { } name)
		{
			item[nameField] = name;
		}
	}
}
