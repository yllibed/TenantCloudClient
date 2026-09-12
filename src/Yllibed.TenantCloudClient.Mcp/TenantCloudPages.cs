using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Repl;

namespace Yllibed.TenantCloudClient.Mcp;

internal static class TenantCloudPages
{
	public static IReplPageSource<JsonObject> Create<T>(IPaginatedSource<T> source, IReplPagingContext paging,
		JsonTypeInfo<T> typeInfo, EntityCache? cache = null) =>
		paging.CreateSource<JsonObject>(async (request, ct) =>
		{
			if (request.AllRequested)
			{
				throw new ArgumentException("Fetching all results is not supported. Follow the continuation cursor instead.", nameof(request));
			}
			var (page, offset, consumed) = ParseCursor(request.Cursor);
			var items = new List<JsonObject>();
			long total;
			while (true)
			{
				ct.ThrowIfCancellationRequested();
				var (entries, actualPage, totalEntries) = await source.GetPage(ct, page).ConfigureAwait(false);
				total = totalEntries;
				if (actualPage != page || offset > entries.Length)
				{
					throw new ArgumentException("The cursor no longer matches the source page. Restart without a cursor.", nameof(request));
				}
				if (entries.IsEmpty || consumed >= total)
				{
					break;
				}
				var take = Math.Min(entries.Length - offset, request.PageSize - items.Count);
				foreach (var entry in entries.Slice(offset, take).ToArray())
				{
					items.Add(JsonSerializer.SerializeToNode(entry, typeInfo)!.AsObject());
				}
				offset += take;
				consumed += take;
				if (offset == entries.Length)
				{
					page++;
					offset = 0;
				}
				if (items.Count == request.PageSize || consumed >= total)
				{
					break;
				}
			}
			if (cache is not null)
			{
				await EntityEnricher.EnrichAsync(items, cache, ct).ConfigureAwait(false);
			}
			var next = items.Count > 0 && consumed < total
				? string.Create(CultureInfo.InvariantCulture, $"{page}:{offset}:{consumed}") : null;
			return request.Page(items, next, total);
		});

	private static (long Page, int Offset, long Consumed) ParseCursor(string? cursor)
	{
		if (cursor is null)
		{
			return (1, 0, 0);
		}
		var parts = cursor.Split(':');
		if (parts.Length != 3
			|| !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var page) || page < 1 || page == long.MaxValue
			|| !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var offset)
			|| !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var consumed) || consumed < offset)
		{
			throw new ArgumentException("Invalid TenantCloud continuation cursor.", nameof(cursor));
		}
		return (page, offset, consumed);
	}
}
