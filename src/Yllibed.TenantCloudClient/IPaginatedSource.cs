using System.Buffers;

namespace Yllibed.TenantCloudClient;

public interface IPaginatedSource<T>
{
	Task<(ReadOnlyMemory<T> entries, long pageNo, long totalEntries)> GetPage(CancellationToken ct, long pageNo = 1);

	Task<ReadOnlySequence<T>> GetAll(CancellationToken ct, long maxResults = 300);
}
