using System;
using Newtonsoft.Json;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcLease
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("amount")]
		public decimal Amount { get; set; }

		[JsonProperty("created_at")]
		public DateTimeOffset CreationDate { get; set; }

		[JsonProperty("rent_from")]
		public DateTime StartDate { get; set; }

		[JsonProperty("rent_to")]
		public DateTime? EndDate { get; set; }

		[JsonProperty("unit_id")]
		public long UnitId { get; set; }
	}
}
