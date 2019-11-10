using System;
using System.Collections.Generic;
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

		/// <summary>
		/// Increments 'read' by the result, but returns 'result' for comparing.
		/// </summary>
		public static int IncButCmp(this int result, ref int read)
		{
			read += result;
			return result;
		}

		public static int NextChar(this ReadOnlySpan<IToken> source, char character)
		{
			int read;
			if (0 == (read = source.NextNoWhitespace<CharacterToken>(out var token))) return 0;
			if (token.Character != character) return 0;

			return read;
		}

		public static int NextNoWhitespace<T>(this ReadOnlySpan<IToken> source, out T token)
			where T : IToken
		{
			var read = NextNoWhitespace(source, out var iToken);

			if (iToken is T tToken)
			{
				token = tToken;
				return read;
			}

			token = default;
			return 0;
		}

		public static int NextNoWhitespace(this ReadOnlySpan<IToken> source, out IToken token)
		{
			token = default;

			for (var i = 0; i < source.Length; i++)
			{
				if (source[i].IsWhitespace()) continue;
				token = source[i];
				return i + 1;
			}

			return 0;
		}
	}
}