using Terumi.Tokens;

namespace Terumi.SyntaxTree.Expressions
{
	public class MethodCall : Expression
	{
		public MethodCall(bool isCompilerMethodCall, IdentifierToken methodName, MethodCallParameterGroup parameters)
		{
			MethodName = methodName;
			Parameters = parameters;
			IsCompilerMethodCall = isCompilerMethodCall;
		}

		public IdentifierToken MethodName { get; }
		public MethodCallParameterGroup Parameters { get; }
		public bool IsCompilerMethodCall { get; }
	}
}