using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode.Optimization
{
	public static class UselessVariableRemover
	{
		public static bool Optimize(List<Instruction> instructions)
		{
			var inUse = new List<int>();
			return ZOom(instructions, inUse);
		}

		private static bool ZOom(List<Instruction> instructions, List<int> inUse)
		{
			var did = false;
			foreach (var i in instructions)
			{
				inUse.AddRange(i.GetUsedVariables());

				if (i is IClauseInstruction o)
				{
					var moreInUse = new List<int>(inUse);
					ZOom(o.Clause, moreInUse);
					moreInUse = moreInUse.Distinct().ToList();
					did = RemoveAllNotInUse(o.Clause, moreInUse) || did;
				}
			}

			inUse = inUse.Distinct().ToList();
			did = RemoveAllNotInUse(instructions, inUse) || did;
			return did;
		}

		private static bool RemoveAllNotInUse(List<Instruction> instructions, List<int> inUse)
		{
			var did = false;
			for (int i = 0; i < instructions.Count; i++)
			{
				var ins = instructions[i];
				switch (ins)
				{
					case Instruction.Load n:
					{
						if (!inUse.Contains(n.Store))
						{
							instructions.RemoveAt(i--);
							did = true;
						}
					}
					break;

					case Instruction.Call o:
					{
						if (o.Store != -1)
							if (!inUse.Contains(o.Store))
							{
								o.Store = -1;
								did = true;
							}
					}
					break;

					case Instruction.CompilerCall o:
					{
						if (o.Store != -1)
							if (!inUse.Contains(o.Store))
							{
								o.Store = -1;
								did = true;
							}
					}
					break;
				}
			}
			return did;
		}
	}
}
