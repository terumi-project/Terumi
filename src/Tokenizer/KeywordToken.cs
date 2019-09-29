namespace Terumi.Tokenizer
{
	public class KeywordToken : Token
	{
		public KeywordToken(Keyword keyword)
			=> Keyword = keyword;

		public Keyword Keyword { get; }
	}
}