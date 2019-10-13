using Terumi.Tokens;

namespace Terumi.SyntaxTree.Expressions
{
	public class MethodCall : Expression
	{
		public MethodCall(IdentifierToken methodName, MethodCallParameterGroup parameters)
		{
			MethodName = methodName;
			Parameters = parameters;
		}

		public IdentifierToken MethodName { get; }
		public MethodCallParameterGroup Parameters { get; }
	}
}