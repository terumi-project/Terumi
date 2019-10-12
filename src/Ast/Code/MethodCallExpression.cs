using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Workspace.TypePasser;

namespace Terumi.Ast.Code
{
	public class MethodCallExpression : ICodeExpression
	{
		public MethodCallExpression(ICodeExpression entity, InfoItem.Method callingMethod, IReadOnlyCollection<ICodeExpression> parameters)
		{
			Entity = entity;
			CallingMethod = callingMethod;
			Parameters = parameters;
		}

		public ICodeExpression Entity { get; }
		public InfoItem.Method CallingMethod { get; }
		public IReadOnlyCollection<ICodeExpression> Parameters { get; }

		public InfoItem Type => CallingMethod.ReturnType;
	}
}
