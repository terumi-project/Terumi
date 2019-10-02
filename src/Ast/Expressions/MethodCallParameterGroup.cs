using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Ast.Expressions
{
	public class MethodCallParameterGroup
	{
		public MethodCallParameterGroup(Expression[] expressions)
		{
			Expressions = expressions;
		}

		public Expression[] Expressions { get; }
	}
}
