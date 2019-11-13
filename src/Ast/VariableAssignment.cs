using Terumi.Binder;

namespace Terumi.Ast
{
	public class VariableAssignment : CodeStatement, ICodeExpression
	{
		public VariableAssignment(string variableName, ICodeExpression value)
		{
			VariableName = variableName;
			Value = value;
		}

		public IType Type => Value.Type;

		public string VariableName { get; set; }
		public ICodeExpression Value { get; set; }
	}
}