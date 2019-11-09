using System;
using Terumi.Tokens;

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

		public static int TryNextNonWhitespace<T>(this Span<IToken> source, out T token)
			where T : IToken
		{
			token = default;

			for(var i = 0; i < source.Length; i++)
			{
				if (source[i].IsWhitespace()) continue;
				if (!(source[i] is T tToken)) continue;
				token = tToken;
				return i + 1;
			}

			return 0;
		}
	}
}