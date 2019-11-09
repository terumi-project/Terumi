using System.Numerics;
using Terumi.Lexer;

namespace Terumi.Tokens
{
	public class NumericToken : Token
	{
		public NumericToken(LexerMetadata meta, BigInteger number)
		{
			Start = meta;
			Number = number;
		}

		public override LexerMetadata Start { get; protected set; }
		public override LexerMetadata End { get; set; }
		public BigInteger Number { get; }
	}
}