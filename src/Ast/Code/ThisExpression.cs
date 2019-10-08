using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Ast.Code
{
	public class ThisExpression : ICodeExpression
	{
		public static ThisExpression Instance { get; } = new ThisExpression();
		public static ICodeExpression IInstance { get; } = Instance;
	}
}
