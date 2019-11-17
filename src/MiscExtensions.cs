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

		/// <summary>
		/// Increments 'read' by the result, but returns 'result' for comparing.
		/// </summary>
		public static int IncButCmp(this int result, ref int read)
		{
			if (result > 0)
			{
				// we don't want -1 to affect the read
				read += result;
			}

			return result;
		}

		public static int NextChar(this ReadOnlySpan<IToken> source, char character)
		{
			int read;
			if (0 == (read = source.NextNoWhitespace<CharacterToken>(out var token, character != '\n'))) return 0;
			if (token.Character != character) return 0;

			return read;
		}

		public static int NextNoWhitespace<T>(this ReadOnlySpan<IToken> source, out T token, bool ignoreNewline = true)
			where T : IToken
		{
			var read = NextNoWhitespace(source, out var iToken, ignoreNewline);

			if (iToken is T tToken)
			{
				token = tToken;
				return read;
			}

			token = default;
			return 0;
		}

		public static int NextNoWhitespace(this ReadOnlySpan<IToken> source, out IToken token, bool ignoreNewline = true)
		{
			token = default;

			for (var i = 0; i < source.Length; i++)
			{
				if (source[i].IsWhitespace(ignoreNewline)) continue;
				token = source[i];
				return i + 1;
			}

			return 0;
		}

		public static bool Next(this ReadOnlySpan<IToken> source, int pos, out IToken token)
		{
			if (source.Length > pos)
			{
				token = source[pos];
				return true;
			}

			token = default;
			return false;
		}
	}
}