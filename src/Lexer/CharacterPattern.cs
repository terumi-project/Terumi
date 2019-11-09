using System;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class CharacterPattern : IPattern
	{
		private readonly char _char;
		private readonly byte _byteChar;

		public CharacterPattern(char character)
		{
			_char = character;
			_byteChar = (byte)character;
		}

		public int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token)
		{
			if (source[0] == _byteChar)
			{
				token = new CharacterToken(meta, _char);
				return 1;
			}

			return 0;
		}
	}
}