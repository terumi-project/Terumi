using System;
using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ThisExpressionPattern : INewPattern<ThisExpression>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ThisExpressionPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public int TryParse(Span<IToken> source, ref ThisExpression item)
		{
			int read;
			if (0 != (read = source.NextNoWhitespace<KeywordToken>(out var token))
				&& token.Keyword == Keyword.This)
			{
				item = ThisExpression.Instance;
				_astNotificationReceiver.AstCreated(source, item);
				return read;
			}

			return 0;
		}
	}
}