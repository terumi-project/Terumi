using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class BooleanLiteralExpressionPattern : IPattern<BooleanLiteralExpression>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public BooleanLiteralExpressionPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public bool TryParse(ReaderFork<Token> source, out BooleanLiteralExpression item)
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

			item = new BooleanLiteralExpression(value);
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}