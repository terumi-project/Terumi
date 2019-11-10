using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ConstantLiteralExpressionBooleanPattern : IPattern<ConstantLiteralExpression<bool>>
	{
		public int TryParse(TokenStream stream, ref ConstantLiteralExpression<bool> item)
		{
			if (!stream.NextNoWhitespace<KeywordToken>(out var keywordToken)) return 0;

			bool value;

			switch (keywordToken.Keyword)
			{
				case Keyword.True: value = true; break;
				case Keyword.False: value = false; break;
				default: return 0;
			}

			item = new ConstantLiteralExpression<bool>(value);
			return stream;
		}
	}
}