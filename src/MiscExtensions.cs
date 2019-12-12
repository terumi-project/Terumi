using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Terumi
{
	public static class MiscExtensions
	{
		public static int MaybeMax<T>(this IEnumerable<T> enumerable, Func<T, int> converter)
			=> enumerable.Select(converter).MaybeMax();

		public static int MaybeMax(this IEnumerable<int> enumerable)
		{
			bool wereElements = false;
			int total = 0;

			using var enumerator = enumerable.GetEnumerator();

			while (enumerator.MoveNext())
			{
				wereElements = true;
				total += enumerator.Current;
			}

			if (!wereElements)
			{
				return -1;
			}

			return total;
		}

		public static bool Any<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, out T result)
		{
			foreach (var item in enumerable)
			{
				if (predicate(item))
				{
					result = item;
					return true;
				}
			}

			result = default;
			return false;
		}

		public static IEnumerable<string> ExcludeLast(this IEnumerable<string> array)
		{
			using var enumerator = array.GetEnumerator();

			if (!enumerator.MoveNext())
			{
				yield break;
			}

			var last = enumerator.Current;

			while (enumerator.MoveNext())
			{
				yield return last;
				last = enumerator.Current;
			}
		}

		public static string Hash(this string input)
		{
			using (var managed = new SHA256Managed())
			{
				var computedBytes = managed.ComputeHash(Encoding.UTF8.GetBytes(input));

				var strb = new StringBuilder(computedBytes.Length * 2);

				foreach (var computedByte in computedBytes)
				{
					strb.AppendFormat("{0:x2}", computedByte);
				}

				return strb.ToString();
			}
		}
	}
}