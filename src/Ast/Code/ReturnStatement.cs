using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Ast.Code
{
	public class ReturnStatement : CodeStatement
	{
		public ReturnStatement(ICodeExpression returnOn)
		{
			ReturnOn = returnOn;
		}

		public ICodeExpression ReturnOn { get; }
	}
}
