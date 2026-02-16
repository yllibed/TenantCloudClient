using System.Globalization;

namespace Yllibed.TenantCloudClient.HttpMessages;

public class TcProperty : IHasId
{
	public long Id { get; set; }

	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("address1")]
	public string? Address1 { get; set; }

	[JsonPropertyName("cityAddress")]
	public string? CityAddress { get; set; }

	[JsonPropertyName("property_status")]
	public string? Status { get; set; }

	public string Address => string.Format(CultureInfo.InvariantCulture, "{0} {1}", Address1, CityAddress);
}
