using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimization
{
	/// <summary>
	/// This optimization will either remove parameter loads entirely, or move
	/// parameter inwards to where they are used. This is to optimize functions
	/// such as the following:
	/// 
	/// <code>
	/// do_thing(number a, number b)
	/// {
	///		// note: this is a terumi representation of varcode
	///		_1 = a
	///		_2 = b
	///		
	///		// code
	///		
	///		if (some_condition()) {
	///			@println("{to_string(_1)}")
	///		}
	/// }
	/// </code>
	/// 
	/// _1 is only used within some_condition, and thus could be loaded there.
	/// _2 is not used at all, and could be completely disregarded.
	/// </summary>
	public class ParameterLoadInlining
	{
		// NOTICE: this optimization only performs the disregarding part of the optimization
		// currently, no parameter calls are inlined at their site of use
		public static bool Peel(Method method)
		{
			var usages = new List<ParameterVarIdUsage>();
			Find(method.Code, usages);

			var used = false;
			foreach (var i in usages)
			{
				if (!i.VariableUsed)
				{
					used = true;
					i.Remove();
				}
			}

			return used;
		}

		private class ParameterVarIdUsage
		{
			public int ParameterIndex { get; set; }
			public int VariableId { get; set; }
			public bool VariableUsed { get; set; }
			public Instruction? UsageSite { get; set; }
			public Action Remove { get; set; }
		}

		// TODO: bool -> instruction to inline parameter use
		private static void Find(List<Instruction> instructions, List<ParameterVarIdUsage> usages)
		{
			foreach (var i in instructions)
			{
				switch (i)
				{
					case Instruction.Load.Parameter o:
					{
						// TODO: scopes on the usages
						usages.Add(new ParameterVarIdUsage
						{
							ParameterIndex = o.ParameterNumber,
							VariableId = o.Store,
							VariableUsed = false,
							UsageSite = null,
							Remove = () => instructions.Remove(o)
						});
					}
					break;

					case Instruction.Assign o when usages.Any(x => !x.VariableUsed && o.Value == x.VariableId, out var p):
					{
						p.VariableUsed = true;
						p.UsageSite = o;
					}
					break;

					case Instruction.GetField o when usages.Any(x => !x.VariableUsed && o.VariableId == x.VariableId, out var p):
					{
						p.VariableUsed = true;
						p.UsageSite = o;
					}
					break;

					case Instruction.SetField o when usages.Any(x => !x.VariableUsed && (o.VariableId == x.VariableId || o.ValueId == x.VariableId), out var p):
					{
						p.VariableUsed = true;
						p.UsageSite = o;
					}
					break;

					case Instruction.Return o when usages.Any(x => !x.VariableUsed && o.ValueId == x.VariableId, out var p):
					{
						p.VariableUsed = true;
						p.UsageSite = o;
					}
					break;

					case Instruction.While o:
					{
						Find(o.Clause, usages);
					}
					break;

					case Instruction.If o:
					{
						Find(o.Clause, usages);
					}
					break;

					case Instruction.Call o:
					{
						foreach (var j in o.Arguments)
						{
							if (usages.Any(x => !x.VariableUsed && x.VariableId == j, out var p))
							{
								p.VariableUsed = true;
								p.UsageSite = o;
							}
						}
					}
					break;

					case Instruction.CompilerCall o:
					{
						foreach (var j in o.Arguments)
						{
							if (usages.Any(x => !x.VariableUsed && x.VariableId == j, out var p))
							{
								p.VariableUsed = true;
								p.UsageSite = o;
							}
						}
					}
					break;
				}
			}
		}
	}
}
