using System;
using System.Runtime.CompilerServices;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class WhitespacePattern : IPattern
	{
		public int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token)
		{
			// short circuit if we don't have any data
			if (!IsWhitespace(source[0]))
			{
				return 0;
			}

			var i = 1;

			while (IsWhitespace(source[i]))
			{
				i++;
			}

			token = new WhitespaceToken(meta);
			return i;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static bool IsWhitespace(byte value)
			=> value == (byte)' '
			|| value == (byte)'\t'
			|| value == (byte)'\r';
	}
}