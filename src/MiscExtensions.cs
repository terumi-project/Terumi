using System;
using System.Collections.Generic;

namespace Terumi
{
	public static class MiscExtensions
	{
		public static string[] ExcludeLast(this string[] array)
		{
			var newArray = new string[array.Length - 1];

			Array.Copy(array, 0, newArray, 0, newArray.Length);

			return newArray;
		}

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
