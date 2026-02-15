using Yllibed.TenantCloudClient.HttpMessages;

namespace Yllibed.TenantCloudClient;

public static class TcContactsPaginatedSourceExtensions
{
	public static IPaginatedSource<TcContact> OnlyTenants(this IPaginatedSource<TcContact> source)
	{
		if (source is PaginatedSource<TcContact> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[roles][]=tenant");
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcContact> OnlyMovedIn(this IPaginatedSource<TcContact> source)
	{
		if (source is PaginatedSource<TcContact> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[tenant_contact_type]=moved_in");
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcContact> OnlyProfessionals(this IPaginatedSource<TcContact> source)
	{
		if (source is PaginatedSource<TcContact> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[roles][]=professional");
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}

	public static IPaginatedSource<TcContact> OnlyArchived(this IPaginatedSource<TcContact> source)
	{
		if (source is PaginatedSource<TcContact> paginatedSource)
		{
			return paginatedSource.ProjectedWithExtraUrl(url => url + "&filter[status]=archived");
		}

		throw new ArgumentException("Invalid source.", nameof(source));
	}
}
