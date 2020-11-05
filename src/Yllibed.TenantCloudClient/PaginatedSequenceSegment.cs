using System;
using System.Buffers;

namespace Yllibed.TenantCloudClient
{
	internal class PaginatedSequenceSegment<T> : ReadOnlySequenceSegment<T>
	{
		internal PaginatedSequenceSegment(ReadOnlyMemory<T> memory, long index)
		{
			Memory = memory;
			RunningIndex = index;
		}

		internal void SetNext(PaginatedSequenceSegment<T> next)
		{
			Next = next;
		}
	}
}
