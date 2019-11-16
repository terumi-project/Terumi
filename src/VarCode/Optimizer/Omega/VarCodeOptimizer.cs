using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode.Optimizer.Omega
{
	public interface IOptimization
	{
		bool Run(VarCodeStore store);
	}

	public class VarCodeOptimizer
	{
		private readonly IOptimization[] _optimizations;

		public VarCodeOptimizer(VarCodeStore store, IOptimization[] optimizations)
		{
			Store = store;
			_optimizations = optimizations;
		}

		public VarCodeStore Store { get; }

		public void Optimize()
		{
			var did = new bool[_optimizations.Length];

			do
			{
				for (var i = 0; i < _optimizations.Length; i++)
				{
					did[i] = _optimizations[i].Run(Store);
				}
			}
			while (did.Any(x => x));
		}
	}
}
