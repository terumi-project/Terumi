using System.Collections.Generic;

namespace Terumi.SyntaxTree.Expressions
{
	public class MethodCallParameterGroup
	{
		public MethodCallParameterGroup(List<Expression> expressions)
			=> Expressions = expressions;

		public List<Expression> Expressions { get; }
	}
}