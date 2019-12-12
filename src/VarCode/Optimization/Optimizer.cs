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
			bool passAgain = false;

			var methods = await PruneMethods.UsedMethods(oldMethods).ConfigureAwait(false);
			passAgain = passAgain || methods.Count < oldMethods.Count;

			// TODO: parallelize?
			foreach (var method in methods)
			{
				passAgain = passAgain || PeelObjects.Peel(method, fieldCount);
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
