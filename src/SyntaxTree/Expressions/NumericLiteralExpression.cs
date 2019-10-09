using System.Numerics;

namespace Terumi.SyntaxTree.Expressions
{
	public class NumericLiteralExpression : LiteralExpression<BigInteger>
	{
		public NumericLiteralExpression(BigInteger number)
			: base(number)
		{
		}
	}
}
