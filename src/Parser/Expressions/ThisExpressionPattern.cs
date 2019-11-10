using System;
using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ThisExpressionPattern : INewPattern<ThisExpression>
	{
		public int TryParse(TokenStream stream, ref ThisExpression item)
		{
			if (stream.NextNoWhitespace<KeywordToken>(out var token)
				&& token.Keyword == Keyword.This)
			{
				item = ThisExpression.Instance;
				return stream;
			}

			return 0;
		}
	}
}