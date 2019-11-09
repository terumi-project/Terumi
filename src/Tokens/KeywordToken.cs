using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class KeywordToken : IToken
	{
		public KeywordToken(LexerMetadata start, Keyword keyword)
		{
			Start = start;
			Keyword = keyword;
		}

		public LexerMetadata Start { get; }
		public LexerMetadata End { get; set; }
		public Keyword Keyword { get; }
	}
}