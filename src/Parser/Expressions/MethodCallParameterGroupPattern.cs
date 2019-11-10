using System;
using System.Collections.Generic;

using Terumi.SyntaxTree.Expressions;

namespace Terumi.Parser.Expressions
{
	public class MethodCallParameterGroupPattern : IPattern<MethodCallParameterGroup>
	{
		public IPattern<Expression> ExpressionPattern { get; set; }

		public int TryParse(TokenStream stream, ref MethodCallParameterGroup item)
		{
			if (ExpressionPattern == null)
			{
				throw new Exception("Must set ExpressionPattern");
			}

			if (!stream.TryParse(ExpressionPattern, out var expression))
			{
				item = new MethodCallParameterGroup(Array.Empty<Expression>());
				return stream;
			}

			var expressions = new List<Expression>(1)
			{
				expression
			};

			while (stream.NextChar(','))
			{
				if (!stream.TryParse(ExpressionPattern, out expression))
				{
					Log.Error($"Expected to be able to parse another expression, but failed {stream.Top.Start}");
					return 0;
				}

				expressions.Add(expression);
			}

			item = new MethodCallParameterGroup(expressions);
			return stream;
		}
	}
}