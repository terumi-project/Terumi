using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class WhitespaceToken : IToken
	{
		public WhitespaceToken(LexerMetadata start)
		{
			Start = start;
		}

		public LexerMetadata Start { get; }
		public LexerMetadata End { get; set; }
	}
}