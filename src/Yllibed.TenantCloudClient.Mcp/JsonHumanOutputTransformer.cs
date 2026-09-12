using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Repl;

namespace Yllibed.TenantCloudClient.Mcp;

internal sealed class JsonHumanOutputTransformer(IOutputTransformer fallback) : IOutputTransformer
{
	private static readonly JsonSerializerOptions DisplayOptions = new()
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public string Name => "json-human";
	public string MimeType => "text/plain";
	public bool SupportsInteractivePaging => true;

	public ValueTask<string> TransformAsync(object? value, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (value is IReplPage page && page.UntypedItems.Count > 0
			&& page.UntypedItems.All(item => item is null or JsonNode or JsonElement))
		{
			var body = string.Join(Environment.NewLine + Environment.NewLine,
				page.UntypedItems.Select(item => RenderJson(ToElement(item), cancellationToken)));
			var info = page.PageInfo;
			var footer = info.TotalCount is { } total
				? string.Create(CultureInfo.InvariantCulture, $"Showing {page.UntypedItems.Count} of {total}.")
				: string.Create(CultureInfo.InvariantCulture, $"Showing {page.UntypedItems.Count} result(s).");
			if (info.HasMore)
			{
				footer += $" Next data page: rerun with --result:cursor {Escape(info.NextCursor!)}.";
			}
			return ValueTask.FromResult(body + Environment.NewLine + footer);
		}
		if (value is JsonNode or JsonElement)
		{
			return ValueTask.FromResult(RenderJson(ToElement(value), cancellationToken));
		}
		return fallback.TransformAsync(value, cancellationToken);
	}

	private static JsonElement ToElement(object? value) => value is JsonElement element
		? element : JsonSerializer.SerializeToElement((JsonNode?)value);

	private static string RenderJson(JsonElement value, CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		if (value.ValueKind == JsonValueKind.Array)
		{
			return value.GetArrayLength() == 0 ? "[]" : string.Join(Environment.NewLine + Environment.NewLine,
				value.EnumerateArray().Select(item => RenderJson(item, ct)));
		}
		if (value.ValueKind != JsonValueKind.Object)
		{
			return JsonSerializer.Serialize(value, DisplayOptions);
		}
		var fields = value.EnumerateObject().ToArray();
		if (fields.Length == 0)
		{
			return "{}";
		}
		var width = fields.Max(field => Escape(field.Name).Length);
		return string.Join(Environment.NewLine, fields.Select(field =>
			Escape(field.Name).PadRight(width) + " : " + JsonSerializer.Serialize(field.Value, DisplayOptions)));
	}

	private static string Escape(string text) => JsonEncodedText.Encode(text, DisplayOptions.Encoder).ToString();
}
