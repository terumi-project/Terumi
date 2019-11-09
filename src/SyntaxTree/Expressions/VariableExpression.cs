using Terumi.Tokens;

namespace Terumi.SyntaxTree.Expressions
{
	public class VariableExpression : Expression
	{
		public VariableExpression(ParameterType type, IdentifierToken identifier, Expression value)
		{
			Type = type;
			Identifier = identifier;
			Value = value;
		}

		public ParameterType Type { get; }
		public IdentifierToken Identifier { get; }
		public Expression Value { get; }
	}
}