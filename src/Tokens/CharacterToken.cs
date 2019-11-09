using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class CharacterToken : Token
	{
		public CharacterToken(LexerMetadata meta, char character)
		{
			Start = meta;
			Character = character;
		}

		public override LexerMetadata Start { get; protected set; }
		public override LexerMetadata End { get; set; }
		public char Character { get; }
	}
}