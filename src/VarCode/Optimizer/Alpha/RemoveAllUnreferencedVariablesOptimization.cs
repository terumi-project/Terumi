using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode.Optimizer.Alpha
{
	public class RemoveAllUnreferencedVariablesOptimization : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			var didRemove = false;

			foreach (var structure in store.Structures)
			{
				var references = Run(structure.Tree.Code);

				didRemove = Run(structure.Tree.Code, references) || didRemove;
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
					case VarReturn o: assignments.Add(o.Id); break;

					case VarAssignment o:
					{
						assignments.AddRange(References(o.Value));
					}
					break;

					case VarMethodCall o:
					{
						if (o.VariableId != null)
						{
							assignments.Add((VarCodeId)o.VariableId);
						}

						assignments.AddRange(References(o.MethodCallVarExpression));
					}
					break;

					case VarIf o:
					{
						assignments.Add(o.ComparisonVariable);
						assignments.AddRange(Run(o.TrueBody));
					}
					break;
				}
			}

			return assignments.Distinct().ToList();

			static IEnumerable<VarCodeId> References(VarExpression expression)
			{
				switch (expression)
				{
					case MethodCallVarExpression o:
					{
						yield return o.MethodId;

						foreach (var id in o.ParameterVariables)
						{
							yield return id;
						}
					}
					break;

					case ReferenceVarExpression o:
					{
						yield return o.VariableId;
					}
					break;
				}
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

					case VarParameterAssignment o:
					{
						if (!usedVars.Contains(o.Id))
						{
							instructions.RemoveAt(i--);
						}
					}
					break;
				}
			}

			return didRemove;
		}
	}
}
