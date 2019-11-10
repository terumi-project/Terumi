using System.Numerics;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ConstantLiteralExpressionBigIntegerPattern : IPattern<ConstantLiteralExpression<BigInteger>>
	{
		public int TryParse(TokenStream stream, ref ConstantLiteralExpression<BigInteger> item)
		{
			if (!stream.NextNoWhitespace<NumericToken>(out var numeric)) return 0;

			item = new ConstantLiteralExpression<BigInteger>(numeric.Number);
			return stream;
		}
	}
}