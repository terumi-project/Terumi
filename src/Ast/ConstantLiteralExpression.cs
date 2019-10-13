using System.Numerics;

using Terumi.Binder;

namespace Terumi.Ast
{
	public class ConstantLiteralExpression<T> : ICodeExpression
	{
		public ConstantLiteralExpression(T value)
		{
			Literal = value;

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

		public T Literal { get; }

		public InfoItem Type { get; }
	}
}