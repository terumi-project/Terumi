using System.Numerics;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ConstantLiteralExpressionBigIntegerPattern : IPattern<ConstantLiteralExpression<BigInteger>>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ConstantLiteralExpressionBigIntegerPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public bool TryParse(ReaderFork<IToken> source, out ConstantLiteralExpression<BigInteger> item)
		{
			if (!source.TryNextNonWhitespace<NumericToken>(out var numeric))
			{
				item = default;
				return false;
			}

			item = new ConstantLiteralExpression<BigInteger>(numeric.Number);
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}