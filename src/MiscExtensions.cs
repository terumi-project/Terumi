using System;
using System.Collections.Generic;

namespace Terumi
{
	public static class MiscExtensions
	{
		public static bool TryFirst<T>(this IEnumerable<T> enumerable, out T item, Predicate<T> isItem)
		{
			using var enumerator = enumerable.GetEnumerator();

			while (enumerator.MoveNext())
			{
				if (isItem(enumerator.Current))
				{
					item = enumerator.Current;
					return true;
				}
			}

			item = default;
			return false;
		}
	}
}
