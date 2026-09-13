using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Yllibed.TenantCloudClient.HttpMessages;
using Yllibed.TenantCloudClient.Mcp;

namespace Yllibed.TenantCloudClient.Tests;

[TestClass]
public sealed class Given_FlexiblePropertyAndUnitJson
{
	private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

	[TestMethod]
	[DataRow("\"active\"", "active")]
	[DataRow("1", "1")]
	[DataRow("-1", "-1")]
	[DataRow("null", null)]
	[DataRow("true", "true")]
	[DataRow("false", "false")]
	[DataRow("12345678901234567890123456789", "12345678901234567890123456789")]
	[DataRow("1.2300", "1.2300")]
	public void When_ReadingPropertyStatus_Then_PreservesScalarValue(string value, string? expected)
	{
		var json = "{\"property_status\":" + value + "}";

		var property = JsonSerializer.Deserialize<TcProperty>(json, Options);

		property.Should().NotBeNull();
		property!.Status.Should().Be(expected);
		using var output = JsonDocument.Parse(JsonSerializer.Serialize(property, Options));
		var status = output.RootElement.GetProperty("property_status");
		status.ValueKind.Should().Be(expected is null ? JsonValueKind.Null : JsonValueKind.String);
		status.GetString().Should().Be(expected);
	}

	[TestMethod]
	[DataRow("null", null)]
	[DataRow("\"\"", null)]
	[DataRow("\"  \"", null)]
	[DataRow("0", "0")]
	[DataRow("1250.50", "1250.50")]
	[DataRow("\"1250.50\"", "1250.50")]
	[DataRow("-12.25", "-12.25")]
	[DataRow("79228162514264337593543950335", "79228162514264337593543950335")]
	public void When_ReadingUnitPrice_Then_PreservesNullableDecimal(string value, string? expected)
	{
		var json = "{\"price\":" + value + "}";
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-CA");
			var unit = JsonSerializer.Deserialize<TcUnit>(json, Options);

			unit.Should().NotBeNull();
			unit!.Price.Should().Be(expected is null ? null : decimal.Parse(expected, CultureInfo.InvariantCulture));
			using var output = JsonDocument.Parse(JsonSerializer.Serialize(unit, Options));
			var price = output.RootElement.GetProperty("Price");
			price.ValueKind.Should().Be(expected is null ? JsonValueKind.Null : JsonValueKind.Number);
			if (expected is not null)
			{
				price.GetDecimal().Should().Be(unit.Price);
			}
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}

	[TestMethod]
	[DataRow("{\"price\":true}")]
	[DataRow("{\"price\":{}}")]
	[DataRow("{\"price\":[]}")]
	[DataRow("{\"price\":\"not-a-price\"}")]
	[DataRow("{\"price\":79228162514264337593543950336}")]
	public void When_UnitPriceIsInvalid_Then_ReportsJsonPath(string json)
	{
		Action act = () => JsonSerializer.Deserialize<TcUnit>(json, Options);

		act.Should().Throw<JsonException>().Which.Path.Should().Be("$.price");
	}

	[TestMethod]
	[DataRow("{}")]
	[DataRow("[]")]
	public void When_PropertyStatusIsStructured_Then_ReportsJsonPath(string value)
	{
		Action act = () => JsonSerializer.Deserialize<TcProperty>("{\"property_status\":" + value + "}", Options);

		act.Should().Throw<JsonException>().Which.Path.Should().Be("$.property_status");
	}

	[TestMethod]
	public void When_UsingMcpSourceGeneration_Then_HandlesVariantPayloads()
	{
		var property = JsonSerializer.Deserialize("{\"property_status\":2}", McpJsonContext.Default.TcProperty);
		var units = new[]
		{
			JsonSerializer.Deserialize("{\"price\":null}", McpJsonContext.Default.TcUnit),
			JsonSerializer.Deserialize("{\"price\":\"12.50\"}", McpJsonContext.Default.TcUnit),
		};

		property!.Status.Should().Be("2");
		units[0]!.Price.Should().BeNull();
		units[1]!.Price.Should().Be(12.50m);
		using var output = JsonDocument.Parse(JsonSerializer.Serialize(units[1], McpJsonContext.Default.TcUnit));
		output.RootElement.GetProperty("price").GetDecimal().Should().Be(12.50m);
	}
}
