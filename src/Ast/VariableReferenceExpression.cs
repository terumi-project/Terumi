using Terumi.Binder;

namespace Terumi.Ast
{
	public class VariableReferenceExpression : ICodeExpression
	{
		public VariableReferenceExpression(string varName, UserType type)
		{
			VarName = varName;
			Type = type;
		}

		public string VarName { get; }
		public UserType Type { get; }
	}
}