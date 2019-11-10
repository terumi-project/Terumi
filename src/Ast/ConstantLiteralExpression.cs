using System.Numerics;

using Terumi.Binder;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Ast
{
	public class ConstantLiteralExpression<T> : Expression, ICodeExpression
	{
		public ConstantLiteralExpression(T value)
		{
			Value = value;

			switch (value)
			{
				case string _:
					Type = CompilerDefined.String;
					break;

				case BigInteger _:
					Type = CompilerDefined.Number;
					break;

				case bool _:
					Type = CompilerDefined.Boolean;
					break;
			}
		}

		public T Value { get; }

		public IType Type { get; }
	}
}