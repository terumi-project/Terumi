using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class NumericLiteralExpressionPattern : IPattern<NumericLiteralExpression>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public NumericLiteralExpressionPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public bool TryParse(ReaderFork<Token> source, out NumericLiteralExpression item)
		{
			if (!source.TryNextNonWhitespace<NumericToken>(out var numeric))
			{
				item = default;
				return false;
			}

			item = new NumericLiteralExpression(numeric.Number);
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}