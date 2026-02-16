using System.Globalization;
using System.Text.Json.Nodes;

namespace Yllibed.TenantCloudClient.Mcp;

/// <summary>
/// Post-processes tool JSON output to append a <c>references</c> lookup table
/// that maps foreign-key IDs to human-readable names.
/// </summary>
internal static class EntityEnricher
{
	/// <summary>
	/// Parses the JSON, collects <c>property_id</c> and <c>unit_id</c> values from the
	/// <c>data</c> array, resolves them via the cache, and appends a <c>references</c>
	/// section to the root object.
	/// </summary>
	public static async Task<string> EnrichAsync(string json, EntityCache cache, CancellationToken ct)
	{
		var node = JsonNode.Parse(json);
		if (node is not JsonObject root || root["data"] is not JsonArray dataArray)
		{
			return json;
		}

		var propertyIds = CollectIds(dataArray, "property_id");
		var unitIds = CollectIds(dataArray, "unit_id");

		var references = new JsonObject();

		await AddReferencesAsync(references, "properties", propertyIds, cache.GetPropertyNameAsync, ct).ConfigureAwait(false);
		await AddReferencesAsync(references, "units", unitIds, cache.GetUnitNameAsync, ct).ConfigureAwait(false);

		if (references.Count > 0)
		{
			root["references"] = references;
		}

		return root.ToJsonString();
	}

	private static HashSet<long> CollectIds(JsonArray dataArray, string fieldName)
	{
		var ids = new HashSet<long>();
		foreach (var item in dataArray)
		{
			if (item is JsonObject obj
				&& obj[fieldName] is JsonValue v
				&& v.TryGetValue<long>(out var id))
			{
				ids.Add(id);
			}
		}

		return ids;
	}

	private static async Task AddReferencesAsync(
		JsonObject references,
		string sectionName,
		HashSet<long> ids,
		Func<long, CancellationToken, Task<string?>> resolver,
		CancellationToken ct)
	{
		if (ids.Count == 0)
		{
			return;
		}

		var section = new JsonObject();
		foreach (var id in ids)
		{
			var name = await resolver(id, ct).ConfigureAwait(false);
			if (name is not null)
			{
				section[id.ToString(CultureInfo.InvariantCulture)] = name;
			}
		}

		if (section.Count > 0)
		{
			references[sectionName] = section;
		}
	}
}
