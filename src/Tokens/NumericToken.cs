using System.Numerics;

using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class NumericToken : IToken
	{
		public NumericToken(LexerMetadata meta, BigInteger number)
		{
			Start = meta;
			Number = number;
		}

		public LexerMetadata Start { get; }
		public LexerMetadata End { get; set; }
		public BigInteger Number { get; }
	}
}