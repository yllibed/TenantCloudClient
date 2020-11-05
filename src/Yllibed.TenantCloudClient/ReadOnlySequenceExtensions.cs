using System;
using System.Buffers;
using System.Collections.Generic;

namespace Yllibed.TenantCloudClient
{
	public static class ReadOnlySequenceExtensions
	{
		public static IEnumerable<T> AsEnumerable<T>(this ReadOnlySequence<T> source)
		{
			var enumerator = source.GetEnumerator();

			while (enumerator.MoveNext())
			{
				var items = enumerator.Current.ToArray();

				foreach (var item in items)
				{
					yield return item;
				}
			}
		}
	}
}
