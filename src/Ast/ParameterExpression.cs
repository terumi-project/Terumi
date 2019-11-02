using Terumi.Binder;

namespace Terumi.Ast
{
	public class ParameterExpression : ICodeExpression
	{
		public ParameterExpression(InfoItem.Method.Parameter parameter)
		{
			Parameter = parameter;
		}

		public InfoItem Type => Parameter.Type;

		public InfoItem.Method.Parameter Parameter { get; }
	}
}
