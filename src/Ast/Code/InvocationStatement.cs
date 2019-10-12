using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Workspace.TypePasser;

namespace Terumi.Ast.Code
{
	public class InvocationStatement : CodeStatement, ICodeExpression
	{
		public InvocationStatement(ICodeExpression primary, ICodeExpression action)
		{
			Primary = primary;
			Action = action;
		}

		public ICodeExpression Primary { get; }
		public ICodeExpression Action { get; }
		public InfoItem Type => Action.Type;
	}
}
