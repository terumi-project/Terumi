using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Ast.Code
{
	public class MethodCallExpression : ICodeExpression
	{
		public MethodCallExpression(ICodeExpression entity, MethodDefinition callingMethod, IReadOnlyCollection<ICodeExpression> parameters)
		{
			Entity = entity;
			CallingMethod = callingMethod;
			Parameters = parameters;
		}

		public ICodeExpression Entity { get; }
		public MethodDefinition CallingMethod { get; }
		public IReadOnlyCollection<ICodeExpression> Parameters { get; }
	}
}
