using System.Globalization;

namespace Yllibed.TenantCloudClient;

public static class TcTransactionsPaginatedSourceExtensions
{
	public static IPaginatedSource<TcTransaction> ForTenant(this IPaginatedSource<TcTransaction> source, long tenantId)
	{
		if (source is PaginatedSource<TcTransaction> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[client_id]=" + tenantId.ToString(NumberFormatInfo.InvariantInfo));
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcTransaction> ForProperty(this IPaginatedSource<TcTransaction> source, long propertyId)
	{
		if (source is PaginatedSource<TcTransaction> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[property_id][]=" + propertyId.ToString(NumberFormatInfo.InvariantInfo));
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcTransaction> ForUnit(this IPaginatedSource<TcTransaction> source, long unitId)
	{
		if (source is PaginatedSource<TcTransaction> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[unit_id]=" + unitId.ToString(NumberFormatInfo.InvariantInfo));
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcTransaction> ForStatus(this IPaginatedSource<TcTransaction> source, TcTransactionStatus status)
	{
		if (source is PaginatedSource<TcTransaction> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[status]=" + status.ToSerializedString());
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcTransaction> ForCategory(this IPaginatedSource<TcTransaction> source, TcTransactionCategory category)
	{
		if (source is PaginatedSource<TcTransaction> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[category][]=" + category.ToString().ToLowerInvariant());
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcTransaction> SortByDateDescending(this IPaginatedSource<TcTransaction> source)
	{
		if (source is PaginatedSource<TcTransaction> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&sort=-date,-id");
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	internal static string ToSerializedString(this TcTransactionStatus status)
	{
		switch (status)
		{
			case TcTransactionStatus.Due:
			case TcTransactionStatus.Paid:
			case TcTransactionStatus.Partial:
			case TcTransactionStatus.Pending:
			case TcTransactionStatus.Void:
				var b = (byte)status;
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
