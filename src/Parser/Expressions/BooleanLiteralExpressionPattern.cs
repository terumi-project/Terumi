using System;
using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ConstantLiteralExpressionBooleanPattern : INewPattern<ConstantLiteralExpression<bool>>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ConstantLiteralExpressionBooleanPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public int TryParse(Span<IToken> source, ref ConstantLiteralExpression<bool> item)
		{
			int consumed;
			if ((consumed = source.TryNextNonWhitespace<KeywordToken>(out var keywordToken)) == 0) return 0;

			bool value;

			switch (keywordToken.Keyword)
			{
				case Keyword.True: value = true; break;
				case Keyword.False: value = false; break;
				default: return 0;
			}

			item = new ConstantLiteralExpression<bool>(value);
			_astNotificationReceiver.AstCreated(source, item);
			return consumed;
		}
	}
}