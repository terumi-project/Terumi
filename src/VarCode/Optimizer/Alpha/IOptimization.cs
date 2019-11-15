using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimizer.Alpha
{
	public interface IOptimization
	{
		bool Run(VarCodeStore store);
	}
}
