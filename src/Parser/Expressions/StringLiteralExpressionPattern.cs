using System;
using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ConstantLiteralExpressionStringPattern : INewPattern<ConstantLiteralExpression<string>>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ConstantLiteralExpressionStringPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public int TryParse(Span<IToken> source, ref ConstantLiteralExpression<string> item)
		{
			int read;
			if (0 == (read = source.NextNoWhitespace<StringToken>(out var @string))) return 0;

			item = new ConstantLiteralExpression<string>(@string.String);
			_astNotificationReceiver.AstCreated(source, item);
			return read;
		}
	}
}