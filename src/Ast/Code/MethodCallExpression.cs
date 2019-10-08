using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Ast.Code
{
	public class MethodCallExpression : ICodeExpression
	{
		public MethodCallExpression(MethodDefinition callingMethod, IReadOnlyCollection<ICodeExpression> parameters)
		{
			CallingMethod = callingMethod;
			Parameters = parameters;
		}

		public MethodDefinition CallingMethod { get; }
		public IReadOnlyCollection<ICodeExpression> Parameters { get; }
	}
}
