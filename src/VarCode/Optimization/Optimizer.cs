using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Terumi.VarCode.Optimization
{
	public static class Optimizer
	{
		public static async ValueTask Optimize(List<Method> oldMethods, int fieldCount)
		{
			var methods = await PruneMethods.UsedMethods(oldMethods).ConfigureAwait(false);
			var passAgain = methods.Count < oldMethods.Count;

			foreach (var method in methods)
			{
				passAgain = PeelObjects.Peel(method, fieldCount) || passAgain;
				// passAgain = PeelParameters.Peel(method, methods) || passAgain;
				passAgain = ParameterLoadInlining.Peel(method) || passAgain;
				passAgain = CompileTimeComputing.Optimize(method.Code) || passAgain;
				passAgain = AssignmentInlining.Optimize(method.Code) || passAgain;
				// passAgain = UselessVariableRemover.Optimize(method.Code) || passAgain;
				passAgain = UnecessaryAssignmentRemover.Optimize(method.Code) || passAgain;
			}

			if (passAgain)
			{
				await Optimize(methods, fieldCount);
				oldMethods.Clear();
				oldMethods.AddRange(methods);
			}
		}
	}
}
