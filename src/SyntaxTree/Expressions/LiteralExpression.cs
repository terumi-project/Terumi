namespace Terumi.SyntaxTree.Expressions
{
	public class LiteralExpression<T> : Expression
	{
		public LiteralExpression(T literalValue)
		{
			LiteralValue = literalValue;
		}

		public T LiteralValue { get; }
	}
}