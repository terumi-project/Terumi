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
					Type = TypeInformation.String;
					break;

				case BigInteger _:
					Type = TypeInformation.Number;
					break;

				case bool _:
					Type = TypeInformation.Boolean;
					break;
			}
		}

		public T Value { get; }

		public UserType Type { get; }
	}
}