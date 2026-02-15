using System.Globalization;

namespace Yllibed.TenantCloudClient;

public static class TcLeasesPaginatedSourceExtensions
{
	public static IPaginatedSource<TcLease> OnlyActive(this IPaginatedSource<TcLease> source)
	{
		if (source is PaginatedSource<TcLease> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[lease_status][]=active");
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcLease> ForProperty(this IPaginatedSource<TcLease> source, long propertyId)
	{
		if (source is PaginatedSource<TcLease> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[property_id][]=" + propertyId.ToString(CultureInfo.InvariantCulture));
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcLease> ForUnit(this IPaginatedSource<TcLease> source, long unitId)
	{
		if (source is PaginatedSource<TcLease> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[unit_id]=" + unitId.ToString(CultureInfo.InvariantCulture));
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}
}
