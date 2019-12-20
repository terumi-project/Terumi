using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode.Optimization
{
	/// <summary>
	/// If something is assigned to twice, eg.
	/// 
	/// a = "hello"
	/// a = "world"
	/// 
	/// we will remove the first assignment since 'a' is never used in any computations
	/// between its first set and second set
	/// </summary>
	public class UnecessaryAssignmentRemover
	{
		public static bool Optimize(List<Instruction> instructions)
		{
			var didOpt = false;

			for (int i = 0; i < instructions.Count; i++)
			{
				var ins = instructions[i];
				if (ins is IClauseInstruction clause)
				{
					didOpt = Optimize(clause.Clause) || didOpt;
					continue;
				}

				if (ins is Instruction.Load load)
				{
					// try to find the next load to the same variable
					var foundNextLoad = false;
					int nextLoad = 0;

					for (int j = i + 1; j < instructions.Count; j++)
					{
						if (instructions[j] is Instruction.Load l && l.Store == load.Store)
						{
							foundNextLoad = true;
							nextLoad = j;
							break;
						}
					}

					if (!foundNextLoad)
					{
						continue;
					}

					// now we know the two indexes of the load

					//  v  ___v
					// [ a, b, a, d ]

					var inbetween = instructions.Skip(i + 1).Take(nextLoad - (i + 1));

					var allComputed = new List<int>();

					void ComputeInner(IClauseInstruction clause)
					{
						foreach (var inst2 in clause.Clause)
						{
							allComputed.AddRange(inst2.GetUsedVariables());

							if (inst2 is IClauseInstruction clauseInstruction)
							{
								ComputeInner(clauseInstruction);
							}
						}
					}

					foreach (var inst in inbetween)
					{
						allComputed.AddRange(inst.GetUsedVariables());

						if (inst is IClauseInstruction o)
						{
							ComputeInner(o);
						}
					}

					allComputed = allComputed.Distinct().ToList();

					if (!allComputed.Contains(load.Store))
					{
						// can remove this unecessary load
						instructions.RemoveAt(i--);
						didOpt = true;
					}
				}
			}

			return didOpt;
		}
	}
}
