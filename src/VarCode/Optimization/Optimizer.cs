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

			// TODO: parallelize?
			foreach (var method in methods)
			{
				passAgain = PeelObjects.Peel(method, fieldCount) || passAgain;
			}

			foreach (var method in methods)
			{
				passAgain = PeelParameters.Peel(method) || passAgain;
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
