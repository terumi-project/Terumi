using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimizer.Alpha
{
	public class BodyFoldingOptimization : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			var couldFold = false;

			foreach (var structure in store.Structures)
			{
				couldFold = Run(structure.Tree.Code) || couldFold;
			}

			return couldFold;
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

					case VarIf o when constantMap.TryGetValue(o.ComparisonVariable, out var constant)
							&& constant is bool b:
					{
						didModify = true;

						// remove the if statement so that we won't have it
						instructions.RemoveAt(i);

						// and if it's true, put it back
						if (b)
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
