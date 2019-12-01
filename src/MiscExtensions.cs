using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

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