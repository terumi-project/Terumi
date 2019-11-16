using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode.Optimizer.Omega
{
	public class RemoveAllUnreferencedVariablesOptimization : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			var didRemove = false;

			foreach (var structure in store.Functions)
			{
				var references = Run(structure.Instructions);

				didRemove = Run(structure.Instructions, references) || didRemove;
			}

			{
				var references = Run(store.Instructions);

				didRemove = Run(store.Instructions, references) || didRemove;
			}

			return didRemove;
		}

		public List<VarCodeId> Run(List<VarInstruction> instructions)
		{
			var assignments = new List<VarCodeId>();

			for (var i = 0; i < instructions.Count; i++)
			{
				switch (instructions[i])
				{
					case VarReturn o: Search(assignments, o.Value); break;
					case VarAssignment o: Search(assignments, o.Value); break;
					case VarMethodCall o: Search(assignments, o.MethodCallVarExpression); break;
					case VarIf o:
					{
						assignments.AddRange(Run(o.TrueBody));
						Search(assignments, o.ComparisonExpression);
					}
					break;
				}
			}

			return assignments.Distinct().ToList();
		}

		private void Search(List<VarCodeId> ids, VarExpression expression)
		{
			switch (expression)
			{
				case ParameterReferenceVarExpression _: break;
				case IConstantVarExpression _: break;
				case MethodCallVarExpression o:
				{
					foreach (var i in o.Parameters)
					{
						Search(ids, i);
					}
				}
				break;
				case ReferenceVarExpression o: ids.Add(o.VariableId); break;
				default: throw new NotImplementedException();
			}
		}

		public bool Run(List<VarInstruction> instructions, List<VarCodeId> usedVars)
		{
			var didRemove = false;

			for (var i = 0; i < instructions.Count; i++)
			{
				switch(instructions[i])
				{
					case VarAssignment o:
					{
						if (!usedVars.Contains(o.VariableId))
						{
							instructions.RemoveAt(i--);
						}
					}
					break;

					case VarIf o:
					{
						didRemove = Run(o.TrueBody, usedVars) || didRemove;
					}
					break;
				}
			}

			return didRemove;
		}
	}
}
