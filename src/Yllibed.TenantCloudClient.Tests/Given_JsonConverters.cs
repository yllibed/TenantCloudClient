using System.Text.Json;
using AwesomeAssertions;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient.Tests;

[TestClass]
public class Given_JsonConverters
{
	private static readonly JsonSerializerOptions Options = new()
	{
		AllowTrailingCommas = true,
		PropertyNameCaseInsensitive = true,
	};

	[TestMethod]
	public void AutoLongConverter_ReadsNumber()
	{
		var json = """{"id": 42}""";
		var result = JsonSerializer.Deserialize<TcTenant>(json, Options);

		result.Should().NotBeNull();
		result!.Id.Should().Be(42);
	}

	[TestMethod]
	public void AutoLongConverter_ReadsString()
	{
		var json = """{"id": "123"}""";
		var result = JsonSerializer.Deserialize<TcTenant>(json, Options);

		result.Should().NotBeNull();
		result!.Id.Should().Be(123);
	}

	[TestMethod]
	public void AutoLongConverter_WritesNumber()
	{
		var tenant = new TcTenant { Id = 99, Name = "Test" };
		var json = JsonSerializer.Serialize(tenant, Options);

		json.Should().Contain("99");
	}

	[TestMethod]
	public void TransactionStatusConverter_ReadsByte()
	{
		var json = """{"id": 1, "status": 1}""";
		var result = JsonSerializer.Deserialize<TcTransaction>(json, Options);

		result.Should().NotBeNull();
		result!.Status.Should().Be(TcTransactionStatus.Paid);
	}

	[TestMethod]
	public void TransactionStatusConverter_ReadsString()
	{
		var json = """{"id": 1, "status": "Paid"}""";
		var result = JsonSerializer.Deserialize<TcTransaction>(json, Options);

		result.Should().NotBeNull();
		result!.Status.Should().Be(TcTransactionStatus.Paid);
	}

	[TestMethod]
	public void DecimalConverter_ReadsNumber()
	{
		var json = """{"id": 1, "amount": 99.50, "paid": 0, "balance": 99.50}""";
		var result = JsonSerializer.Deserialize<TcTransaction>(json, Options);

		result.Should().NotBeNull();
		result!.Amount.Should().Be(99.50m);
	}

	[TestMethod]
	public void DateConverter_ReadsDateString()
	{
		var json = """{"id": 1, "date": "1/15/2024", "amount": 0, "paid": 0, "balance": 0}""";
		var result = JsonSerializer.Deserialize<TcTransaction>(json, Options);

		result.Should().NotBeNull();
		result!.DueDate.Month.Should().Be(1);
		result.DueDate.Day.Should().Be(15);
		result.DueDate.Year.Should().Be(2024);
	}

	[TestMethod]
	public void NullableDateConverter_ReadsNull()
	{
		var json = """{"id": 1, "paid_at": null, "date": "1/1/2024", "amount": 0, "paid": 0, "balance": 0}""";
		var result = JsonSerializer.Deserialize<TcTransaction>(json, Options);

		result.Should().NotBeNull();
		result!.PaidAt.Should().BeNull();
	}
}
