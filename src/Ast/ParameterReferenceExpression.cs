using Terumi.Binder;

namespace Terumi.Ast
{
	public class ParameterReferenceExpression : ICodeExpression
	{
		public ParameterReferenceExpression(InfoItem.Method.Parameter parameter)
		{
			Parameter = parameter;
		}

		public InfoItem Type => Parameter.Type;

		public InfoItem.Method.Parameter Parameter { get; }
	}
}
