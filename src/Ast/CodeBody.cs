using Terumi.Ast.Expressions;

namespace Terumi.Ast
{
	public class CodeBody
	{
		public CodeBody(Expression[] expressions)
		{
			Expressions = expressions;
		}

		public Expression[] Expressions { get; }
	}
}