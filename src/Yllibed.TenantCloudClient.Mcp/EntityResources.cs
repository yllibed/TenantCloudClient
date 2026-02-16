using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;

namespace Yllibed.TenantCloudClient.Mcp;

[McpServerResourceType]
internal sealed class EntityResources
{
	[McpServerResource(
		UriTemplate = "tc://property/{id}",
		Name = "property",
		Title = "Property details by ID",
		MimeType = "application/json")]
	[Description("Look up a property by its numeric ID. Returns name, address, and status.")]
	public static async Task<string> GetProperty(
		long id,
		EntityCache cache,
		CancellationToken ct)
	{
		var property = await cache.GetPropertyAsync(id, ct).ConfigureAwait(false);
		if (property is null)
		{
			return NotFound("property", id);
		}

		var obj = new JsonObject
		{
			["id"] = property.Id,
			["name"] = property.Name,
			["address"] = property.Address,
			["status"] = property.Status,
		};

		return obj.ToJsonString();
	}

	[McpServerResource(
		UriTemplate = "tc://unit/{id}",
		Name = "unit",
		Title = "Unit details by ID",
		MimeType = "application/json")]
	[Description("Look up a rental unit by its numeric ID. Returns name, property, price, and occupancy.")]
	public static async Task<string> GetUnit(
		long id,
		EntityCache cache,
		CancellationToken ct)
	{
		var unit = await cache.GetUnitAsync(id, ct).ConfigureAwait(false);
		if (unit is null)
		{
			return NotFound("unit", id);
		}

		var propertyName = await cache.GetPropertyNameAsync(unit.PropertyId, ct).ConfigureAwait(false);

		var obj = new JsonObject
		{
			["id"] = unit.Id,
			["name"] = unit.Name,
			["propertyId"] = unit.PropertyId,
			["propertyName"] = propertyName,
			["price"] = unit.Price,
			["isRented"] = unit.IsRented,
		};

		return obj.ToJsonString();
	}

	[McpServerResource(
		UriTemplate = "tc://contact/{id}",
		Name = "contact",
		Title = "Contact details by ID",
		MimeType = "application/json")]
	[Description("Look up a contact by its numeric ID. Returns name, emails, and phones.")]
	public static async Task<string> GetContact(
		long id,
		EntityCache cache,
		CancellationToken ct)
	{
		var contact = await cache.GetContactAsync(id, ct).ConfigureAwait(false);
		if (contact is null)
		{
			return NotFound("contact", id);
		}

		var obj = new JsonObject
		{
			["id"] = contact.Id,
			["name"] = contact.Name,
			["firstName"] = contact.FirstName,
			["lastName"] = contact.LastName,
			["emails"] = new JsonArray(contact.ValidEmails
				.Where(e => e is not null)
				.Select(e => (JsonNode)JsonValue.Create(e)!)
				.ToArray()),
			["phones"] = new JsonArray(contact.ValidPhones
				.Select(p => (JsonNode)JsonValue.Create(p)!)
				.ToArray()),
		};

		return obj.ToJsonString();
	}

	private static string NotFound(string entity, long id) =>
		string.Create(CultureInfo.InvariantCulture, $"{entity} {id} not found");
}
