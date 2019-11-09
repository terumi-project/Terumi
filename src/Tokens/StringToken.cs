using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class StringToken : IToken
	{
		public StringToken(LexerMetadata meta, string @string)
		{
			Start = meta;
			String = @string;
		}

		public LexerMetadata Start { get; }
		public LexerMetadata End { get; set; }

		public string String { get; }
	}
}
