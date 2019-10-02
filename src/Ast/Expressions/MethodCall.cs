using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Tokens;

namespace Terumi.Ast.Expressions
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
