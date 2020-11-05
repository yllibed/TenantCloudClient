using System;
using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcTransaction
	{
		[JsonPropertyName("id")]
		[JsonConverter(typeof(JsonAutoLongConverter))]
		public long Id { get; set; }

		[JsonPropertyName("unit_id")]
		[JsonConverter(typeof(JsonAutoNullableLongConverter))]
		public long? UnitId { get; set; }

		[JsonPropertyName("property_id")]
		[JsonConverter(typeof(JsonAutoNullableLongConverter))]
		public long? PropertyId { get; set; }

		[JsonPropertyName("detail")]
		public string? Detail { get; set; }

		[JsonPropertyName("is_recurring")]
		public bool IsRecurring { get; set; }

		[JsonPropertyName("created_at")]
		public DateTimeOffset CreatedAt { get; set; }

		[JsonConverter(typeof(JsonStringDateToDateTimeOffsetConverter))]
		[JsonPropertyName("date")]
		public DateTimeOffset DueDate { get; set; }

		[JsonConverter(typeof(JsonDecimalConverter))]
		public decimal Amount { get; set; }

		[JsonConverter(typeof(JsonDecimalConverter))]
		public decimal Paid { get; set; }

		[JsonConverter(typeof(JsonDecimalConverter))]
		public decimal Balance { get; set; }

		public string Currency { get; set; } = string.Empty;

		[JsonConverter(typeof(JsonStringDateToNullableDateTimeOffsetConverter))]
		[JsonPropertyName("paid_at")]
		public DateTimeOffset? PaidAt { get; set; }

		[JsonConverter(typeof(JsonStringToEnumConverter<TcTransactionCategory>))]
		public TcTransactionCategory Category { get; set; }

		[JsonConverter(typeof(JsonTcTransactionStatusConverter))]
		public TcTransactionStatus Status { get; set; }
	}

	public enum TcTransactionCategory : byte
	{
		Income,
		Expense,
		Refund,
		Credits,
		liability
	}

	public enum TcTransactionStatus : byte
	{
		Due = 0,
		Paid = 1,
		Partial=2,
		Pending=3,
		Void = 9,
		WithBalance, // with_balance
		Overdue, // overdue
		Waive, // waive
	}
}
