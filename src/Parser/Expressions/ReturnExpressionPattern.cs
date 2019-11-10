using System;

using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class ReturnExpressionPattern : IPattern<ReturnExpression>
	{
		public IPattern<Expression> ExpressionPattern { get; set; }

		public int TryParse(TokenStream stream, ref ReturnExpression item)
		{
			if (ExpressionPattern == null) throw new Exception("Must set ExpressionPattern.");
			if (!stream.NextKeyword(Keyword.Return)) return 0;

			if (!stream.TryParse(ExpressionPattern, out var expression))
			{
				Log.Error($"Expected an expression to return on, but tt parse an expression {stream.TopInfo}");
				return 0;
			}

			item = new ReturnExpression(expression);
			return stream;
		}
	}
}