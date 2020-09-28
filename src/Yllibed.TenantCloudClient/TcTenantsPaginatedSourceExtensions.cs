using System;
using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient
{
	public static class TcTenantsPaginatedSourceExtensions
	{
		public static IPaginatedSource<TcTenantDetails> OnlyMovedIn(this IPaginatedSource<TcTenantDetails> source)
		{
			if (source is PaginatedSource<TcTenantDetails> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&display=moved_in");
			}

			throw new ArgumentException("Invalid source.");
		}

		public static IPaginatedSource<TcTenantDetails> OnlyArchived(this IPaginatedSource<TcTenantDetails> source)
		{
			if (source is PaginatedSource<TcTenantDetails> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&display=archived");
			}

			throw new ArgumentException("Invalid source.");
		}

		public static IPaginatedSource<TcTenantDetails> OnlyNoLease(this IPaginatedSource<TcTenantDetails> source)
		{
			if (source is PaginatedSource<TcTenantDetails> paginatedSource)
			{
				return paginatedSource.ProjectedWithExtraUrl(url => url + "&display=no_lease");
			}

			throw new ArgumentException("Invalid source.");
		}
	}
}
