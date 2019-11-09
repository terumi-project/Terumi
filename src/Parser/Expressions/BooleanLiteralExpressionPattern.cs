using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ConstantLiteralExpressionBooleanPattern : IPattern<ConstantLiteralExpression<bool>>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ConstantLiteralExpressionBooleanPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public bool TryParse(ReaderFork<IToken> source, out ConstantLiteralExpression<bool> item)
		{
			if (!source.TryNextNonWhitespace<KeywordToken>(out var boolean))
			{
				item = default;
				return false;
			}

			bool value;

			if (boolean.Keyword == Keyword.True)
			{
				value = true;
			}
			else if (boolean.Keyword == Keyword.False)
			{
				value = false;
			}
			else
			{
				item = default;
				return false;
			}

			item = new ConstantLiteralExpression<bool>(value);
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}