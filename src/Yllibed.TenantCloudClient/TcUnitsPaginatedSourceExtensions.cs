using System;
using System.Globalization;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient
{
	public static class TcUnitsPaginatedSourceExtensions
	{
		public static IPaginatedSource<TcUnit> OnlyOccuped(this IPaginatedSource<TcUnit> source)
		{
			if (source is PaginatedSource<TcUnit> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&display=occuped");
			}

			throw new ArgumentException("Invalid source.");
		}

		public static IPaginatedSource<TcUnit> OnlyVacant(this IPaginatedSource<TcUnit> source)
		{
			if (source is PaginatedSource<TcUnit> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&display=vacant");
			}

			throw new ArgumentException("Invalid source.");
		}

		public static IPaginatedSource<TcUnit> ForProperty(this IPaginatedSource<TcUnit> source, long propertyId)
		{
			if (source is PaginatedSource<TcUnit> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&property=" + propertyId.ToString(NumberFormatInfo.InvariantInfo));
			}

			throw new ArgumentException("Invalid source.");
		}
	}
}
