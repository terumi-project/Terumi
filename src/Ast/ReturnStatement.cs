using Terumi.Binder;

namespace Terumi.Ast
{
	public class ReturnStatement : CodeStatement, ICodeExpression
	{
		public ReturnStatement(ICodeExpression returnOn)
		{
			ReturnOn = returnOn;
		}

		public ICodeExpression ReturnOn { get; }

		public IType Type => ReturnOn.Type;
	}
}