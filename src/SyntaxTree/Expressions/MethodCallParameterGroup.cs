using System.Collections.Generic;

namespace Terumi.SyntaxTree.Expressions
{
	public class MethodCallParameterGroup
	{
		public MethodCallParameterGroup(List<Expression> expressions)
			: this(expressions.ToArray())
		{
		}

		public MethodCallParameterGroup(Expression[] expressions)
			=> Expressions = expressions;

		public Expression[] Expressions { get; }
	}
}