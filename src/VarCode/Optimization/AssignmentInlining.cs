using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimization
{
	/// <summary>
	/// This optimization removes unnecessary assignments. An assignment is
	/// deemed unecessary if its value is obtained from the assignment of
	/// another variable, and the previous value was never assigned to.
	/// 
	/// An example of where it can optimize would be the following:
	/// 
	/// a = 0
	/// b = 100
	/// c = TRUE
	/// while c
	///		indir = "a"
	///		indir2 = indir <-- could be removed, <-----------+
	///		write-host indir2 <-- replaced with indir as per |
	///		d = 1
	///		a = a + d
	///		o = a < b
	///		o = k <-- k never used, replace all FUTURE uses of 'o' with 'k'
	///		c = o
	///	
	/// </summary>
	public class AssignmentInlining
	{
		public class Scope
		{
			private readonly Scope? _parent;

			public Scope(Scope? parent = null)
			{
				_parent = parent;
			}

			public Dictionary<int, List<Instruction>> UsedVariables { get; } = new Dictionary<int, List<Instruction>>();

			public void Register(Instruction instruction)
			{
				// when we register a variable, we only register the variables
				// that are used in the expressions, not the results.

				switch (instruction)
				{
					case Instruction.Call o:
					{
						foreach (var i in o.Arguments)
						{
							Register(i, o);
						}
					}
					break;

					case Instruction.CompilerCall o:
					{
						foreach (var i in o.Arguments)
						{
							Register(i, o);
						}
					}
					break;

					case Instruction.GetField o:
					{
						Register(o.VariableId, o);
					}
					break;

					case Instruction.If o:
					{
						Register(o.ComparisonId, o);
					}
					break;

					case Instruction.Return o:
					{
						Register(o.ValueId, o);
					}
					break;

					case Instruction.SetField o:
					{
						Register(o.ValueId, o);
					}
					break;

					case Instruction.While o:
					{
						Register(o.ComparisonId, o);
					}
					break;
				}
			}

			public void Register(int id, Instruction instruction)
			{
				if (UsedVariables.ContainsKey(id))
				{
					UsedVariables[id].Add(instruction);
				}
				else
				{
					UsedVariables[id] = new List<Instruction>
					{
						instruction
					};
				}
			}

			public bool WasUsed(int id)
			{
				return UsedVariables.ContainsKey(id) || ParentUsed(id);
			}

			public bool ParentUsed(int id) => _parent?.WasUsed(id) == true;
		}

		private static bool ReplaceFuture(List<Instruction> body, int k, int targetId, int rewriteId)
		{
			var didOptimize = false;

			for (int j = k; j < body.Count; j++)
			{
				var ins = body[j];

				switch (ins)
				{
					case Instruction.Call o:
					{
						for (int i = 0; i < o.Arguments.Count; i++)
						{
							if (o.Arguments[i] == targetId)
							{
								o.Arguments[i] = rewriteId;
								didOptimize = true;
							}
						}
					}
					break;

					case Instruction.CompilerCall o:
					{
						for (int i = 0; i < o.Arguments.Count; i++)
						{
							if (o.Arguments[i] == targetId)
							{
								o.Arguments[i] = rewriteId;
								didOptimize = true;
							}
						}
					}
					break;

					case Instruction.GetField o:
					{
						if (o.VariableId == targetId)
						{
							o.VariableId = rewriteId;
							didOptimize = true;
						}
					}
					break;

					case Instruction.If o:
					{
						if (o.ComparisonId == targetId)
						{
							o.ComparisonId = rewriteId;
							didOptimize = true;
						}
					}
					break;

					case Instruction.Return o:
					{
						if (o.ValueId == targetId)
						{
							o.ValueId = rewriteId;
							didOptimize = true;
						}
					}
					break;

					case Instruction.SetField o:
					{
						if (o.ValueId == targetId)
						{
							o.ValueId = rewriteId;
							didOptimize = true;
						}
					}
					break;

					case Instruction.While o:
					{
						if (o.ComparisonId == targetId)
						{
							o.ComparisonId = rewriteId;
							didOptimize = true;
						}
					}
					break;
				}
			}

			return didOptimize;
		}

		public static bool Optimize(List<Instruction> body, Scope? scope = null)
		{
			var didOptimize = false;

			scope = scope ?? new Scope();

			for (var i = body.Count - 1; i >= 0; i--)
			{
				var ins = body[i];

				scope.Register(ins);

				switch (ins)
				{
					case Instruction.Assign o:
					{
						// assignments are where we make our decisions if we can inline something
						// if we never used the value, we can remove the assignment and update all uses of an instruction to the value it was being set to

						// eg.
						// a = ""
						// b = a <-- after point, 'a' has never been used
						//       ^ we can replace all instances of 'b' with 'a'
						// @println(b)

						// a = ""
						// if something
						//     c = "b"
						//     a = c <-- *cannot* replace A with C, because A is used below
						// @println(a)

						if (!scope.WasUsed(o.Value) && !scope.ParentUsed(o.Store))
						{
							var didRepl = ReplaceFuture(body, i + 1, o.Store, o.Value);
							didOptimize = didRepl || didOptimize;

							if (didRepl)
							{
								body.RemoveAt(i);
							}
						}
					}
					break;

					case IClauseInstruction o:
					{
						var tmpScope = new Scope(scope);
						didOptimize = Optimize(o.Clause, tmpScope) || didOptimize;
					}
					break;
				}
			}

			return didOptimize;
		}
	}
}
