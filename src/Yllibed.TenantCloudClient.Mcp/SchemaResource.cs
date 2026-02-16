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

		## Entities

		### UserInfo
		Current signed-in user profile.
		- `id` (long) — User ID
		- `email` (string) — Email address
		- `firstName`, `lastName` (string) — Name
		- `company` (string) — Company name
		- `phone` (string) — Phone number
		- `address1`, `address2`, `city`, `state`, `zip` (string) — Address

		### Contact
		A tenant, professional, or other contact.
		- `id` (long) — Contact ID
		- `name`, `firstName`, `lastName` (string) — Name
		- `email1`, `email2`, `email3` (string) — Up to 3 email addresses
		- `phone1`, `phone2`, `phone3` (string) — Up to 3 phone numbers
		- `status` (TcTenantStatus) — Contact status

		### Property
		A rental property.
		- `id` (long) — Property ID
		- `name` (string) — Property name
		- `address1` (string) — Street address
		- `cityAddress` (string) — City and state
		- `status` (string) — Property status

		### Unit
		A rental unit within a property.
		- `id` (long) — Unit ID
		- `propertyId` (long) — Parent property ID
		- `name` (string) — Unit name
		- `description` (string?) — Description
		- `price` (decimal) — Rent price
		- `isRented` (bool) — Currently occupied
		- `isPetAllowed` (bool) — Pets allowed
		- `isFurnished` (bool) — Furnished
		- `isUtilities` (bool) — Utilities included

		### Transaction
		A financial transaction (rent, expense, etc.).
		- `id` (long) — Transaction ID
		- `unitId` (long?) — Associated unit
		- `propertyId` (long?) — Associated property
		- `detail` (string?) — Description
		- `amount` (decimal) — Total amount
		- `paid` (decimal) — Amount paid
		- `balance` (decimal) — Remaining balance
		- `currency` (string) — Currency code
		- `dueDate` (DateTimeOffset) — Due date
		- `paidAt` (DateTimeOffset?) — Payment date
		- `category` (TcTransactionCategory) — Income, Expense, Refund, Credits, Liability
		- `status` (TcTransactionStatus) — Due, Paid, Partial, Pending, Void, WithBalance, Overdue, Waive
		- `isRecurring` (bool) — Recurring transaction

		### Lease
		A lease agreement.
		- `id` (long) — Lease ID
		- `name` (string?) — Lease name
		- `unitId` (long) — Associated unit
		- `startDate` (DateTime) — Start date
		- `endDate` (DateTime?) — End date (null = month-to-month)
		- `status` (TcLeaseStatus) — Active, Archived, Ended, Expired, Future, Pending, etc.

		## Tool Filters

		### list_contacts
		- `role` (string?) — Filter by role: `tenant`, `professional`, `moved_in`, `archived`
		- `maxResults` (int?) — Max results to return (default 100)

		### list_properties
		- `maxResults` (int?) — Max results to return (default 100)

		### list_units
		- `propertyId` (long?) — Filter by property ID
		- `occupancy` (string?) — Filter by occupancy: `occupied`, `vacant`
		- `maxResults` (int?) — Max results to return (default 100)

		### list_transactions
		- `tenantId` (long?) — Filter by tenant/contact ID
		- `propertyId` (long?) — Filter by property ID
		- `unitId` (long?) — Filter by unit ID
		- `status` (string?) — Filter by status: `due`, `paid`, `partial`, `pending`, `void`, `with_balance`, `overdue`, `waive`
		- `category` (string?) — Filter by category: `income`, `expense`, `refund`, `credits`, `liability`
		- `maxResults` (int?) — Max results to return (default 100)

		### list_leases
		- `propertyId` (long?) — Filter by property ID
		- `unitId` (long?) — Filter by unit ID
		- `status` (string?) — Filter by status: `active`
		- `maxResults` (int?) — Max results to return (default 100)

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
		- "Who are my tenants?" → list_contacts with role=tenant
		- "What properties do I have?" → list_properties
		- "Which units are vacant?" → list_units with occupancy=vacant
		- "What is the rent balance for property X?" → resolve property name → list_transactions with propertyId + status=with_balance
		- "Show me active leases for [address]" → resolve property → list_leases with propertyId + status=active
		- "Show transactions for [tenant name]" → resolve contact → list_transactions with tenantId
		- "Who am I logged in as?" → get_user_info
		""";
}
