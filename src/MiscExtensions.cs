using System;
using System.Collections.Generic;

namespace Terumi
{
	public static class MiscExtensions
	{
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
	}
}