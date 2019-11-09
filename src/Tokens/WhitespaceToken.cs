using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class WhitespaceToken : Token
	{
		public WhitespaceToken(LexerMetadata start)
		{
			Start = start;
		}

		public override LexerMetadata Start { get; protected set; }
		public override LexerMetadata End { get; set; }
	}
}