using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Terumi.Tokenizer;

namespace Terumi.Lexer
{
	public class WhitespacePattern : IPattern
	{
		public bool TryParse(ReaderFork source, out Token token)
		{
			if (!source.TryNext(out var value)
			|| !IsWhitespace(value))
			{
				token = default;
				return false;
			}

			int start = source.Position;

			while (source.TryNext(out value))
			{
				if (!IsWhitespace(value))
				{
					source.Back(1);

					token = new WhitespaceToken(start, source.Position);
					return true;
				}
			}

			token = default;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static bool IsWhitespace(byte value)
			=> value == ' '
			|| value == '\t'
			|| value == '\r';
	}
}
