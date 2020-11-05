using System;
using System.Globalization;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient
{
	public static class TcTransactionsPaginatedSourceExtensions
	{
		public static IPaginatedSource<TcTransaction> ForTenant(this IPaginatedSource<TcTransaction> source, long tenantId)
		{
			if (source is PaginatedSource<TcTransaction> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&client=" + tenantId.ToString(NumberFormatInfo.InvariantInfo));
			}

			throw new ArgumentException("Invalid source.");
		}

		public static IPaginatedSource<TcTransaction> ForProperty(this IPaginatedSource<TcTransaction> source, long propertyId)
		{
			if (source is PaginatedSource<TcTransaction> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&property=" + propertyId.ToString(NumberFormatInfo.InvariantInfo));
			}

			throw new ArgumentException("Invalid source.");
		}

		public static IPaginatedSource<TcTransaction> ForUnit(this IPaginatedSource<TcTransaction> source, long unitId)
		{
			if (source is PaginatedSource<TcTransaction> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&unit=" + unitId.ToString(NumberFormatInfo.InvariantInfo));
			}

			throw new ArgumentException("Invalid source.");
		}

		public static IPaginatedSource<TcTransaction> ForStatus(this IPaginatedSource<TcTransaction> source, TcTransactionStatus status)
		{
			if (source is PaginatedSource<TcTransaction> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&status=" + status.ToSerializedString());
			}

			throw new ArgumentException("Invalid source.");
		}

		public static IPaginatedSource<TcTransaction> ForCategory(this IPaginatedSource<TcTransaction> source, TcTransactionCategory category)
		{
			if (source is PaginatedSource<TcTransaction> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&category=" + category.ToString().ToLowerInvariant());
			}

			throw new ArgumentException("Invalid source.");
		}

		internal static string ToSerializedString(this TcTransactionStatus status)
		{
			switch(status)
			{
				case TcTransactionStatus.Due:
				case TcTransactionStatus.Paid:
				case TcTransactionStatus.Partial:
				case TcTransactionStatus.Pending:
				case TcTransactionStatus.Void:
					var b = (byte) status;
					return b.ToString(NumberFormatInfo.InvariantInfo);
				case TcTransactionStatus.WithBalance:
					return "with_balance";
				case TcTransactionStatus.Overdue:
					return "overdue";
				case TcTransactionStatus.Waive:
					return "waive";
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown status");
			}
		}

	}
}
