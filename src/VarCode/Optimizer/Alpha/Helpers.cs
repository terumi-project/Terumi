using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimizer.Alpha
{
	public static class Helpers
	{
		public static VarExpression? TryExpression(this VarInstruction instruction)
			=> instruction switch
			{
				VarAssignment o => o.Value,
				VarMethodCall o => o.MethodCallVarExpression,
				_ => default
			};
	}
}
