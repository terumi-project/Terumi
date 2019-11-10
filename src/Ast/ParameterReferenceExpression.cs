using Terumi.Binder;

namespace Terumi.Ast
{
	public class ParameterReferenceExpression : ICodeExpression
	{
		public ParameterReferenceExpression(ParameterBind parameter)
		{
			Parameter = parameter;
		}

		public IType Type => Parameter.Type;

		public ParameterBind Parameter { get; }
	}
}