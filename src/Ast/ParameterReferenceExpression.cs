using Terumi.Binder;

namespace Terumi.Ast
{
	public class ParameterReferenceExpression : ICodeExpression
	{
		public ParameterReferenceExpression(MethodBind.Parameter parameter)
		{
			Parameter = parameter;
		}

		public InfoItem Type => Parameter.Type;

		public MethodBind.Parameter Parameter { get; }
	}
}