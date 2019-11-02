namespace Terumi.SyntaxTree.Expressions
{
	public class StringLiteralExpression : LiteralExpression<string>
	{
		public StringLiteralExpression(string literalValue)
			: base(literalValue)
		{
		}
	}
}