using Terumi.Lexer;

namespace Terumi.Tokens
{
	public interface IToken
	{
		public LexerMetadata Start { get; }
		public LexerMetadata End { get; set; }
	}
}