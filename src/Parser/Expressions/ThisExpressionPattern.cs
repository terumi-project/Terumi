using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ThisExpressionPattern : IPattern<ThisExpression>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ThisExpressionPattern
		(
			IAstNotificationReceiver astNotificationReceiver
		)
		{
			_astNotificationReceiver = astNotificationReceiver;
		}

		public bool TryParse(ReaderFork<Token> source, out ThisExpression item)
		{
			if (!source.TryPeekNonWhitespace<KeywordToken>(out var token, out var peeked)
				&& token.Keyword != Keyword.This)
			{
				item = default;
				return false;
			}

			source.Advance(peeked);

			item = ThisExpression.Instance;
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}