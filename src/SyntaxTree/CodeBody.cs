using Terumi.SyntaxTree.Expressions;

namespace Terumi.SyntaxTree
{
	public class CodeBody
	{
		public CodeBody(Expression[] expressions)
			=> Expressions = expressions;

		public Expression[] Expressions { get; }
	}
}