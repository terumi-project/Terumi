using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class CharacterToken : IToken
	{
		public CharacterToken(LexerMetadata meta, char character)
		{
			Start = meta;
			Character = character;
		}

		public LexerMetadata Start { get; }
		public LexerMetadata End { get; set; }
		public char Character { get; }
	}
}