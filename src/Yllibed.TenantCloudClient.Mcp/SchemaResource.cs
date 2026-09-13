using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Yllibed.TenantCloudClient.Mcp;

[McpServerResourceType]
internal sealed class SchemaResource
{
	[McpServerResource(
		UriTemplate = "tc://guide",
		Name = "guide",
		Title = "TenantCloud Tool Usage Guide",
		MimeType = "text/markdown")]
	[Description("Guide for using TenantCloud tools: entities, fields, filters, and how to resolve names to IDs.")]
	public static string GetSchema() => Schema;

	private const string Schema = """
		# TenantCloud Tool Usage Guide

		## Output Presentation — CRITICAL

		You MUST present **human-readable names** to the user, never raw numeric IDs.

		List tools return a paged envelope with `items` and `pageInfo`.
		Use `_replPageSize` to request a bounded page and pass `pageInfo.nextCursor`
		back as `_replCursor` to continue with the same tool and filters. A null next
		cursor means the end. Follow every page before calculating totals or claiming
		that a list is complete. Results are not a snapshot: source changes may affect paging.

		Rows with foreign-key IDs include resolved names when available:
		`property_name`, `unit_name`, `user_client_name`, and `user_payer_name`.
		Prefer these names when presenting results.

		For detailed entity information, read the MCP resources:
		- `tc://property/{id}` — full property details
		- `tc://unit/{id}` — full unit details (includes parent property name)
		- `tc://contact/{id}` — full contact details

		When a reference is missing (entity not found in cache), resolve it by calling
		the corresponding list tool (`list_properties`, `list_units`, `list_contacts`).

		## Entities

		These are the exact JSON field names returned by list tools.

		### UserInfo
		Current signed-in user profile.
		- `id` (long) — User ID
		- `email` (string) — Email address
		- `firstName`, `lastName` (string) — Name
		- `subDomain` (string) — TenantCloud subdomain
		- `company` (string) — Company name
		- `phone` (string) — Phone number
		- `address1`, `address2`, `city`, `state`, `zip` (string) — Address
		- `isCompany` (bool), `fax`, `lang`, `isVip` (string) — Account metadata

		### Contact
		A tenant, professional, or other contact.
		- `id` (long) — Contact ID
		- `name`, `firstName`, `lastName` (string) — Name
		- `email`, `email_2`, `email_3` (string) — Up to 3 email addresses
		- `phone`, `phone_2`, `phone_3` (string) — Up to 3 phone numbers
		- `validEmails`, `emails`, `validPhones`, `phones` — Normalized contact values
		- `status` (TcTenantStatus) — Contact status

		### Property
		A rental property.
		- `id` (long) — Property ID
		- `name` (string) — Property name
		- `address1` (string) — Street address
		- `cityAddress` (string) — City and state
		- `property_status` (string) — Property status
		- `address` (string) — Combined address

		### Unit
		A rental unit within a property.
		- `id` (long) — Unit ID
		- `property_id` (long) — Parent property ID
		- `name` (string) — Unit name
		- `description` (string?) — Description
		- `price` (decimal?) — Rent price; missing or null means unknown, not zero
		- `is_rented` (bool) — Currently occupied
		- `pets_allowed` (bool) — Pets allowed
		- `is_furnished` (bool) — Furnished
		- `is_utilities` (bool) — Utilities included

		### Transaction
		A financial transaction (rent, expense, etc.).
		- `id` (long) — Transaction ID
		- `unit_id` (long?) — Associated unit
		- `property_id` (long?) — Associated property
		- `user_payer_id` (long?) — Payer contact ID (use `user_payer_name` when available)
		- `detail` (string?) — Description
		- `amount` (decimal) — Total amount
		- `paid` (decimal) — Amount paid
		- `balance` (decimal) — Remaining balance
		- `currency` (string) — Currency code
		- `date` (DateTimeOffset) — Due date
		- `paid_at` (DateTimeOffset?) — Payment date
		- `created_at` (DateTimeOffset) — Creation date
		- `category` (TcTransactionCategory) — Income, Expense, Refund, Credits, Liability
		- `status` (TcTransactionStatus) — Due, Paid, Partial, Pending, Void, WithBalance, Overdue, Waive
		- `is_recurring` (bool) — Recurring transaction

		### Lease
		A lease agreement.
		- `id` (long) — Lease ID
		- `name` (string?) — Lease name
		- `unit_id` (long) — Associated unit
		- `user_client_id` (long?) — Tenant contact ID (use `user_client_name` when available)
		- `rent_from` (DateTime) — Start date
		- `rent_to` (DateTime?) — End date (null = month-to-month)
		- `move_out_date` (DateTime?) — Actual move-out date (null if still active)
		- `created_at` (DateTimeOffset) — Creation date
		- `lease_status` (TcLeaseStatus) — Active, Archived, Ended, Expired, Future, Pending, etc.
		- `isArchived` (bool) — Whether the lease is archived

		## Tool Filters — CRITICAL

		You MUST use server-side filters whenever the user's question targets a specific
		property, unit, tenant, or status. **NEVER fetch all data and filter client-side**
		when a filter parameter exists for the criterion. Server-side filtering is faster,
		returns less data, and avoids hitting pagination limits.

		For example, if the user asks about a specific unit:
		1. Resolve the unit ID (see ID Resolution below)
		2. Pass `unitId` to `list_leases` and `list_transactions` — do NOT call these
		   tools without filters and then search through the results yourself.

		### list_contacts
		- `role` (string?) — Filter by role: `tenant`, `professional`, `moved_in`, `archived`

		### list_properties
		No business filters; use the common paging parameters.

		### list_units
		- `propertyId` (long?) — Filter by property ID
		- `occupancy` (string?) — Filter by occupancy: `occupied`, `vacant`

		### list_transactions
		- `tenantId` (long?) — Filter by tenant/contact ID
		- `propertyId` (long?) — Filter by property ID
		- `unitId` (long?) — Filter by unit ID
		- `status` (string?) — Filter by status: `due`, `paid`, `partial`, `pending`, `void`, `with_balance`, `overdue`, `waive`
		- `category` (string?) — Filter by category: `income`, `expense`, `refund`, `credits`, `liability`

		### list_leases
		- `propertyId` (long?) — Filter by property ID
		- `unitId` (long?) — Filter by unit ID
		- `status` (string?) — Filter by status: `active`

		## ID Resolution — IMPORTANT

		Users refer to entities by **name**, not by ID. All filter parameters that accept an ID
		(propertyId, unitId, tenantId) require a numeric ID. You must resolve names to IDs first.

		### Resolution patterns

		**Property by name/address** → call `list_properties`, then match by `name` or `address1` fields.
		**Unit by name** → call `list_units` (optionally filtered by propertyId), then match by `name`.
		**Tenant by name** → call `list_contacts` with role=tenant, then match by `name`, `firstName`, or `lastName`.

		### Multi-step example

		User asks: "List active leases for my property on Evergreen Terrace"
		1. Call `list_properties` → find the property where `address1` or `name` contains "Evergreen Terrace" → get its `id`
		2. Call `list_leases` with `propertyId=<resolved id>` and `status=active`

		User asks: "Show transactions for John Smith"
		1. Call `list_contacts` with `role=tenant` → find contact where `name` contains "John Smith" → get its `id`
		2. Call `list_transactions` with `tenantId=<resolved id>`

		User asks: "Which tenants are in unit 3B at Maple Heights?"
		1. Call `list_properties` → find "Maple Heights" → get its `id`
		2. Call `list_units` with `propertyId=<property id>` → find "3B" → get its `id`
		3. Call `list_leases` with `unitId=<unit id>` and `status=active` → get tenant info from lease names

		Always resolve in order: property → unit → tenant/lease/transaction.
		When the match is ambiguous, present the candidates and ask the user to clarify.

		## Typical Questions

		Always resolve names to IDs first, then use the appropriate filters.

		- "Who are my tenants?" → `list_contacts` with `role=tenant`
		- "What properties do I have?" → `list_properties`
		- "Which units are vacant?" → `list_units` with `occupancy=vacant`
		- "Which units are vacant at [property]?" → resolve property → `list_units` with `propertyId` + `occupancy=vacant`
		- "What is the rent balance for property X?" → resolve property → `list_transactions` with `propertyId` + `status=with_balance`
		- "Show me active leases for [address]" → resolve property → `list_leases` with `propertyId` + `status=active`
		- "Show transactions for [tenant name]" → resolve contact → `list_transactions` with `tenantId`
		- "Show overdue rent for unit 3B at [property]" → resolve property → resolve unit → `list_transactions` with `unitId` + `status=overdue` + `category=income`
		- "What leases are on unit [name]?" → resolve unit → `list_leases` with `unitId`
		- "Show all expenses for [property]" → resolve property → `list_transactions` with `propertyId` + `category=expense`
		- "Who am I logged in as?" → `get_user_info`
		""";
}
