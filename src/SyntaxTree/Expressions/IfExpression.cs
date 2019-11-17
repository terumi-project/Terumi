namespace Terumi.SyntaxTree.Expressions
{
	public class IfExpression : Expression
	{
		public IfExpression(Expression comparison, CodeBody @true, CodeBody? @false)
		{
			Comparison = comparison;
			True = @true;
			False = @false;
		}

		public Expression Comparison { get; }
		public CodeBody True { get; }
		public CodeBody? False { get; }
	}
}
