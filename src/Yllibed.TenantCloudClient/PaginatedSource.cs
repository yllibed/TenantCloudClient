using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.TenantCloudClient
{
	internal class PaginatedSource<T> : IPaginatedSource<T>
	{
		private readonly Func<CancellationToken, long, string, Task<(ReadOnlyMemory<T>, long, long)>> _pageGetter;
		private readonly string _extraUrl;

		public PaginatedSource(Func<CancellationToken, long, string, Task<(ReadOnlyMemory<T>, long, long)>> pageGetter, string extraUrl)
		{
			_pageGetter = pageGetter;
			_extraUrl = extraUrl;
		}

		public Task<(ReadOnlyMemory<T> entries, long pageNo, long totalEntries)> GetPage(CancellationToken ct, long pageNo = 1)
		{
			return _pageGetter(ct, pageNo, _extraUrl);
		}

		public async Task<ReadOnlySequence<T>> GetAll(CancellationToken ct, long maxResults)
		{
			var pageNo = 1;

			PaginatedSequenceSegment<T>? first = null;
			PaginatedSequenceSegment<T>? last = null;

			var index = 0;

			while (!ct.IsCancellationRequested && index <= maxResults)
			{
				var (entries, _, totalEntries) = await GetPage(ct, pageNo++);

				var segment = new PaginatedSequenceSegment<T>(entries, index);
				index += entries.Length;

				if (first == null)
				{
					first = segment;
					last = segment;
				}
				else
				{
					last?.SetNext(segment);
					last = segment;
				}

				if (index >= totalEntries)
				{
					break; // finished
				}
			}

			return first == null
				? ReadOnlySequence<T>.Empty
				: new ReadOnlySequence<T>(first, 0, last, (int)last!.Memory.Length);
		}


		internal PaginatedSource<T> ProjectedWithExtraUrl(Func<string, string> extraUrlUpdater)
		{
			return new PaginatedSource<T>(_pageGetter, extraUrlUpdater(_extraUrl));
		}
	}
}
