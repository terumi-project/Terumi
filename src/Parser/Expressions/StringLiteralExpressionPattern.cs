using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class StringLiteralExpressionPattern : IPattern<StringLiteralExpression>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public StringLiteralExpressionPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public bool TryParse(ReaderFork<Token> source, out StringLiteralExpression item)
		{
			if (!source.TryNextNonWhitespace<StringToken>(out var @string))
			{
				item = default;
				return false;
			}

			item = new StringLiteralExpression(@string.String);
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}