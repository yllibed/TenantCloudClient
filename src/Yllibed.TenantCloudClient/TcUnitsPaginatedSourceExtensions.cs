using System.Globalization;

namespace Yllibed.TenantCloudClient;

public static class TcUnitsPaginatedSourceExtensions
{
	public static IPaginatedSource<TcUnit> OnlyOccuped(this IPaginatedSource<TcUnit> source)
	{
		if (source is PaginatedSource<TcUnit> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[is_rented]=true");
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcUnit> OnlyVacant(this IPaginatedSource<TcUnit> source)
	{
		if (source is PaginatedSource<TcUnit> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[is_rented]=false");
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcUnit> ForProperty(this IPaginatedSource<TcUnit> source, long propertyId)
	{
		if (source is PaginatedSource<TcUnit> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[property_id][]=" + propertyId.ToString(NumberFormatInfo.InvariantInfo));
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}
}
