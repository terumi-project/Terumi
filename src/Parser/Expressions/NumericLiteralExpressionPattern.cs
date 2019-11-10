using System;
using System.Numerics;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ConstantLiteralExpressionBigIntegerPattern : INewPattern<ConstantLiteralExpression<BigInteger>>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ConstantLiteralExpressionBigIntegerPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public int TryParse(Span<IToken> source, ref ConstantLiteralExpression<BigInteger> item)
		{
			int read;
			if (0 == (read = source.NextNoWhitespace<NumericToken>(out var numeric))) return 0;

			item = new ConstantLiteralExpression<BigInteger>(numeric.Number);
			_astNotificationReceiver.AstCreated(source, item);
			return read;
		}
	}
}