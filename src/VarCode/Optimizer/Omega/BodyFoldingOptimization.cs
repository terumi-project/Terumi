using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimizer.Omega
{
	public class BodyFoldingOptimization : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			var couldFold = false;

			foreach (var structure in store.Functions)
			{
				couldFold = Run(structure.Instructions) || couldFold;
			}

			return Run(store.Instructions) || couldFold;
		}

		public bool Run(List<VarInstruction> instructions)
		{
			var didModify = false;

			var constantMap = new Dictionary<VarCodeId, object>();

			for (var i = 0; i < instructions.Count; i++)
			{
				switch (instructions[i])
				{
					case VarAssignment o when o.Value is IConstantVarExpression c:
						constantMap[o.VariableId] = c.Value;
						break;

					case VarIf o when o.ComparisonExpression is ConstantVarExpression<bool> constant:
					{
						didModify = true;

						// remove the if statement so that we won't have it
						instructions.RemoveAt(i);

						// and if it's true, put it back
						if (constant.Value)
						{
							// take everything inside the if statement
							// and insert it into the instructions
							instructions.InsertRange(i, o.TrueBody);
						}

						i--;
					}
					break;

					case VarIf o: didModify = Run(o.TrueBody) || didModify; break;
				}
			}

			return didModify;
		}
	}
}
