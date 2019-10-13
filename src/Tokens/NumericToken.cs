using System.Numerics;

namespace Terumi.Tokens
{
	public class NumericToken : Token
	{
		public NumericToken(BigInteger number)
			=> Number = number;

		public BigInteger Number { get; }
	}
}