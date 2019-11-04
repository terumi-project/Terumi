using System.Numerics;

namespace Terumi.SyntaxTree.Expressions
{
	public class BooleanLiteralExpression : LiteralExpression<bool>
	{
		public BooleanLiteralExpression(bool value)
			: base(value)
		{
		}
	}
}