using System.ComponentModel;
using System.Text.Json.Nodes;
using Repl;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

internal sealed class TransactionTools(ITcClient client, EntityCache cache)
{
	public IReplPageSource<JsonObject> ListTransactions(
		IReplPagingContext paging,
		[Description("Filter by tenant/contact ID")] long? tenantId = null,
		[Description("Filter by property ID")] long? propertyId = null,
		[Description("Filter by unit ID")] long? unitId = null,
		[Description("Filter by status: due, paid, partial, pending, void, with_balance, overdue, waive")] string? status = null,
		[Description("Filter by category: income, expense, refund, credits, liability")] string? category = null)
	{
		var source = client.Transactions;

		if (tenantId.HasValue)
		{
			source = source.ForTenant(tenantId.Value);
		}

		if (propertyId.HasValue)
		{
			source = source.ForProperty(propertyId.Value);
		}

		if (unitId.HasValue)
		{
			source = source.ForUnit(unitId.Value);
		}

		if (status is not null && TryParseTransactionStatus(status, out var parsedStatus))
		{
			source = source.ForStatus(parsedStatus);
		}

		if (category is not null && TryParseTransactionCategory(category, out var parsedCategory))
		{
			source = source.ForCategory(parsedCategory);
		}

		return TenantCloudPages.Create(source, paging, McpJsonContext.Default.TcTransaction, cache);
	}

	private static bool TryParseTransactionStatus(string value, out TcTransactionStatus result)
	{
		result = value.ToLowerInvariant() switch
		{
			"due" => TcTransactionStatus.Due,
			"paid" => TcTransactionStatus.Paid,
			"partial" => TcTransactionStatus.Partial,
			"pending" => TcTransactionStatus.Pending,
			"void" => TcTransactionStatus.Void,
			"with_balance" => TcTransactionStatus.WithBalance,
			"overdue" => TcTransactionStatus.Overdue,
			"waive" => TcTransactionStatus.Waive,
			_ => default,
		};

		return value.ToLowerInvariant() is "due" or "paid" or "partial" or "pending" or "void"
		or "with_balance" or "overdue" or "waive";
	}

	private static bool TryParseTransactionCategory(string value, out TcTransactionCategory result)
	{
		result = value.ToLowerInvariant() switch
		{
			"income" => TcTransactionCategory.Income,
			"expense" => TcTransactionCategory.Expense,
			"refund" => TcTransactionCategory.Refund,
			"credits" => TcTransactionCategory.Credits,
			"liability" => TcTransactionCategory.Liability,
			_ => default,
		};

		return value.ToLowerInvariant() is "income" or "expense" or "refund" or "credits" or "liability";
	}
}
