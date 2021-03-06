﻿using System;
using System.Text.Json.Serialization;

namespace Yllibed.TenantCloudClient.HttpMessages
{
	public class TcLease
	{
		[JsonPropertyName("id")]
		public long Id { get; set; }

		[JsonPropertyName("name")]
		public string? Name { get; set; }

		[JsonPropertyName("created_at")]
		public DateTimeOffset CreationDate { get; set; }

		[JsonPropertyName("rent_from")]
		public DateTime StartDate { get; set; }

		[JsonPropertyName("rent_to")]
		public DateTime? EndDate { get; set; }

		[JsonPropertyName("unit_id")]
		public long UnitId { get; set; }

		[JsonPropertyName("lease_status")]
		public TcLeaseStatus Status { get; set; }

		[JsonIgnore]
		public bool IsPending => Status == TcLeaseStatus.InsurancePending || Status == TcLeaseStatus.Pending;

		public bool IsArchived => Status == TcLeaseStatus.Archived;

		/// <summary>
		/// Checks if this lease is currently active.
		/// </summary>
		/// <param name="referenceDate">Reference date to use for check.  Default to "now"</param>
		public bool GetIsActive(DateTimeOffset? referenceDate = null)
		{
			if (IsArchived || Status == TcLeaseStatus.NotActive)
			{
				return false;
			}

			var dto = referenceDate ?? DateTimeOffset.Now;
			return Status == TcLeaseStatus.Active && !GetIsFuture(dto) && !GetIsPast(dto);
		}

		/// <summary>
		/// Checks if this lease will begin in the future.
		/// </summary>
		/// <param name="referenceDate">Reference date to use for check.  Default to "now"</param>
		public bool GetIsFuture(DateTimeOffset? referenceDate = null)
		{
			if (IsArchived)
			{
				return false;
			}

			var dto = referenceDate ?? DateTimeOffset.Now;
			return StartDate > dto;
		}

		/// <summary>
		/// Checks if this lease is terminated in the past.
		/// </summary>
		/// <param name="referenceDate">Reference date to use for check.  Default to "now"</param>
		public bool GetIsPast(DateTimeOffset? referenceDate = null)
		{
			if (Status == TcLeaseStatus.Expired || Status == TcLeaseStatus.Archived || Status == TcLeaseStatus.Ended)
			{
				return true;
			}

			var dto = referenceDate ?? DateTimeOffset.Now;
			return StartDate < dto && EndDate != null && EndDate < dto;
		}
	}
}
