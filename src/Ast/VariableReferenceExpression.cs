using Terumi.Binder;

namespace Terumi.Ast
{
	public class VariableReferenceExpression : ICodeExpression
	{
		public VariableReferenceExpression(string varName, IType type)
		{
			VarName = varName;
			Type = type;
		}

		public string VarName { get; }
		public IType Type { get; }
	}
}