using System.Collections.Generic;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.SyntaxTree
{
	public class CodeBody
	{
		public CodeBody(List<Expression> expressions)
			=> Expressions = expressions.ToArray();

		public Expression[] Expressions { get; }
	}
}