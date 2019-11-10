namespace Terumi.SyntaxTree.Expressions
{
	public class IfExpression : Expression
	{
		public IfExpression(Expression comparison, CodeBody @true)
		{
			Comparison = comparison;
			True = @true;
		}

		public Expression Comparison { get; }
		public CodeBody True { get; }
	}
}
