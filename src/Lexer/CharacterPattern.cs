using System;
using System.Linq;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class CharacterPattern : IPattern
	{
		private readonly char[] _chars;
		private readonly byte[] _byteChars;

		public CharacterPattern(params char[] characters)
		{
			_chars = characters;
			_byteChars = _chars.Select(x => (byte)x).ToArray();
		}

		public int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token)
		{
			for (var i = 0; i < _byteChars.Length; i++)
			{
				if (source[0] == _byteChars[i])
				{
					token = new CharacterToken(meta, _chars[i]);
					return 1;
				}
			}

			return 0;
		}
	}
}