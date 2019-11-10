using System;
using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ConstantLiteralExpressionStringPattern : INewPattern<ConstantLiteralExpression<string>>
	{
		public int TryParse(TokenStream stream, ref ConstantLiteralExpression<string> item)
		{
			if (!stream.NextNoWhitespace<StringToken>(out var @string)) return 0;

			item = new ConstantLiteralExpression<string>(@string.String);
			return stream;
		}
	}
}