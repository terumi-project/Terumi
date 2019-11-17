using System;
using System.Collections.Generic;
using System.Text;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Parser.Expressions
{
	// this has extremely tight integration with ExpressionPattern
	public class ComparisonExpressionPattern : IPattern<ComparisonExpression>
	{
		public Expression Left { get; set; }
		public IPattern<Expression> ExpressionPattern { get; set; }

		public int TryParse(TokenStream stream, ref ComparisonExpression item)
		{
			ComparisonOperator op;

			if (stream.NextChars("==")) op = ComparisonOperator.Equals;
			else if (stream.NextChars("!=")) op = ComparisonOperator.NotEquals;
			else if (stream.NextChars("<=")) op = ComparisonOperator.LessThanOrEqualTo;
			else if (stream.NextChars(">=")) op = ComparisonOperator.GreaterThanOrEqualTo;
			else if (stream.NextChar('<')) op = ComparisonOperator.LessThan;
			else if (stream.NextChar('>')) op = ComparisonOperator.GreaterThan;
			else return 0;

			if (!stream.TryParse(ExpressionPattern, out var right))
			{
				Log.Error($"Couldn't parse comparison expression {stream.Top}");
				return 0;
			}

			item = new ComparisonExpression(Left, op, right);
			return stream;
		}
	}
}
