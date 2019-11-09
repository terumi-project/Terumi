using Terumi.Binder;

namespace Terumi.Ast
{
	public class VariableReferenceExpression : ICodeExpression
	{
		public VariableReferenceExpression(string varName, InfoItem type)
		{
			VarName = varName;
			Type = type;
		}

		public string VarName { get; }
		public InfoItem Type { get; }
	}
}