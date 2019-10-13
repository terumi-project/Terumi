using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.SyntaxTree.Expressions
{
	public class ThisExpression : Expression
	{
		public static ThisExpression Instance { get; } = new ThisExpression();
	}
}
