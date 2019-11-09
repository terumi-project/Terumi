using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class KeywordToken : Token
	{
		public KeywordToken(LexerMetadata start, Keyword keyword)
		{
			Start = start;
			Keyword = keyword;
		}

		public override LexerMetadata Start { get; protected set; }
		public override LexerMetadata End { get; set; }
		public Keyword Keyword { get; }
	}
}