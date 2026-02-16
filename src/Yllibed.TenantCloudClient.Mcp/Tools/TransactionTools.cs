using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Mcp.Tools;

[McpServerToolType]
internal sealed class TransactionTools
{
	[McpServerTool(Name = "list_transactions"), Description("List financial transactions from TenantCloud. Can filter by tenant, property, unit, status, or category.")]
	public static async Task<CallToolResult> ListTransactions(
		ITcClient client,
		[Description("Filter by tenant/contact ID")] long? tenantId,
		[Description("Filter by property ID")] long? propertyId,
		[Description("Filter by unit ID")] long? unitId,
		[Description("Filter by status: due, paid, partial, pending, void, with_balance, overdue, waive")] string? status,
		[Description("Filter by category: income, expense, refund, credits, liability")] string? category,
		[Description("Maximum number of results to return (default 100)")] int? maxResults,
		CancellationToken ct)
	{
		try
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

			var data = await source.GetAll(ct, maxResults ?? 100).ConfigureAwait(false);
			var result = new ListResult<TcTransaction>(data.AsEnumerable().ToArray());
			return ToolResults.Success(JsonSerializer.Serialize(result, McpJsonContext.Default.ListResultTcTransaction));
		}
		catch (TcClientException ex)
		{
			return ToolResults.Error($"{ex.Message} (HTTP {(int)ex.HttpStatus})");
		}
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
