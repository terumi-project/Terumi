using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ConstantLiteralExpressionStringPattern : IPattern<ConstantLiteralExpression<string>>
	{
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public ConstantLiteralExpressionStringPattern(IAstNotificationReceiver astNotificationReceiver)
			=> _astNotificationReceiver = astNotificationReceiver;

		public bool TryParse(ReaderFork<IToken> source, out ConstantLiteralExpression<string> item)
		{
			if (!source.TryNextNonWhitespace<StringToken>(out var @string))
			{
				item = default;
				return false;
			}

			item = new ConstantLiteralExpression<string>(@string.String);
			_astNotificationReceiver.AstCreated(source, item);
			return true;
		}
	}
}