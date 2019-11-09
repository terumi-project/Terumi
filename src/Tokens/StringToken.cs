using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class StringToken : Token
	{
		public StringToken(LexerMetadata meta, string @string)
		{
			Start = meta;
			String = @string;
		}

		public override LexerMetadata Start { get; protected set; }
		public override LexerMetadata End { get; set; }

		public string String { get; }
	}
}
