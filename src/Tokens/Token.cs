using Terumi.Lexer;

namespace Terumi.Tokens
{
	public abstract class Token
	{
		public abstract LexerMetadata Start { get; protected set; }
		public abstract LexerMetadata End { get; set; }
	}
}