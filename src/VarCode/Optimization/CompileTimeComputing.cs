using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Targets;

namespace Terumi.VarCode.Optimization
{
	/// <summary>
	/// This optimization takes some of the more generic compile time methods
	/// and computes them at compile time, if possible.
	/// </summary>
	public class CompileTimeComputing
	{
		public class Scope
		{
			private readonly Scope? _previous;
			private readonly Dictionary<int, Instruction> _values = new Dictionary<int, Instruction>();

			public Scope(Scope? previous)
			{
				_previous = previous;
			}


			public Instruction GetValue(int id)
			{
				if (_values.TryGetValue(id, out var instruction))
				{
					return instruction;
				}

				if (_previous == null)
				{
					throw new InvalidOperationException();
				}

				return _previous.GetValue(id);
			}

			public void Update(Instruction current)
			{
				switch (current)
				{
					case IResultInstruction result:
					{
						_values[result.Store] = current;
					}
					break;
				}
			}
		}

		public static bool Optimize(List<Instruction> instructions)
		{
			return Hunt(new Scope(null), instructions, new List<int>());
		}

		private static void TryOptimize<T>(string name, Scope scope, Instruction.CompilerCall o, Func<T, int, Instruction?> optimize, Action<Instruction> replace)
		{
			if (o.CompilerMethod.Name != name) return;
			if (o.Arguments.Count != 1) return;

			var instruction = scope.GetValue(o.Arguments[0]);

			if (typeof(T) == typeof(Number)
				&& instruction is Instruction.Load.Number j)
			{
				var instr = optimize((T)(object)j, o.Store);

				if (instr != null)
				{
					replace(instr);
				}
			}
			else if (typeof(T) == typeof(string)
				&& instruction is Instruction.Load.String j2)
			{
				var instr = optimize((T)(object)j2.Value, o.Store);

				if (instr != null)
				{
					replace(instr);
				}
			}
			else if (typeof(T) == typeof(bool)
				&& instruction is Instruction.Load.Boolean j3)
			{
				var instr = optimize((T)(object)j3.Value, o.Store);

				if (instr != null)
				{
					replace(instr);
				}
			}
		}

		private static void TryOptimize<T>(string name, Scope scope, Instruction.CompilerCall o, Func<T, T, int, Instruction?> optimize, Action<Instruction> replace)
		{
			if (o.CompilerMethod.Name != name) return;
			if (o.Arguments.Count != 2) return;

			var instruction = scope.GetValue(o.Arguments[0]);
			var instruction2 = scope.GetValue(o.Arguments[1]);

			if (typeof(T) == typeof(Number)
				&& instruction is Instruction.Load.Number j1
				&& instruction2 is Instruction.Load.Number j12)
			{
				var instr = optimize((T)(object)j1.Value, (T)(object)j12.Value, o.Store);

				if (instr != null)
				{
					replace(instr);
				}
			}
			else if (typeof(T) == typeof(string)
				&& instruction is Instruction.Load.String j2
				&& instruction2 is Instruction.Load.String j22)
			{
				var instr = optimize((T)(object)j2.Value, (T)(object)j22.Value, o.Store);

				if (instr != null)
				{
					replace(instr);
				}

			}
			else if (typeof(T) == typeof(bool)
				&& instruction is Instruction.Load.Boolean j3
				&& instruction2 is Instruction.Load.Boolean j32)
			{
				var instr = optimize((T)(object)j3.Value, (T)(object)j32.Value, o.Store);

				if (instr != null)
				{
					replace(instr);
				}
			}
		}

		private static bool Hunt(Scope scope, List<Instruction> instructions, List<int> previousAssigned)
		{
			// first, find all the variables that get assigned
			var assigned = new List<int>();
			foreach (var i in instructions)
				if (i is Instruction.Assign a)
				{
					// 'store' is getting used,
					// and we're counting the value here as a temporary workaround for loops.
					assigned.Add(a.Store);
					assigned.Add(a.Value);
				}
				else if (i is IResultInstruction o)
					assigned.Add(o.Store);

			var all = new List<int>(assigned.Concat(previousAssigned));

			var canFold = all.GroupBy(x => x)
				.Where(x => x.Count() == 1).Select(x => x.First()).ToList();

			var didOpt = false;

			for (int index = 0; index < instructions.Count; index++)
			{
				var i = instructions[index];
				switch (i)
				{
					case Instruction.CompilerCall o:
					{
						if (!canFold.Contains(o.Store)) break;

						// we expect all compiler implementations to behave roughly the same.
						TryOptimize1(TargetMethodNames.OperatorNegate, (Number n, int store) => new Instruction.Load.Number(store, new Number((System.Numerics.BigInteger)0 - n.Value)));
						TryOptimize1(TargetMethodNames.OperatorNot, (bool b, int store) => new Instruction.Load.Boolean(store, !b));
						TryOptimize<Number>(TargetMethodNames.OperatorAdd, (a, b, s) => new Instruction.Load.Number(s, new Number(a.Value + b.Value)));
						TryOptimize<Number>(TargetMethodNames.OperatorDivide, (a, b, s) => new Instruction.Load.Number(s, new Number(a.Value / b.Value)));
						TryOptimize<Number>(TargetMethodNames.OperatorExponent, (a, b, s) => new Instruction.Load.Number(s, new Number(System.Numerics.BigInteger.Pow(a.Value, (int)b.Value))));
						TryOptimize<Number>(TargetMethodNames.OperatorMultiply, (a, b, s) => new Instruction.Load.Number(s, new Number(a.Value * b.Value)));
						TryOptimize<Number>(TargetMethodNames.OperatorSubtract, (a, b, s) => new Instruction.Load.Number(s, new Number(a.Value - b.Value)));
						TryOptimize<Number>(TargetMethodNames.OperatorGreaterThan, (a, b, s) => new Instruction.Load.Boolean(s, a.Value > b.Value));
						TryOptimize<Number>(TargetMethodNames.OperatorGreaterThanOrEqualTo, (a, b, s) => new Instruction.Load.Boolean(s, a.Value >= b.Value));
						TryOptimize<Number>(TargetMethodNames.OperatorLessThan, (a, b, s) => new Instruction.Load.Boolean(s, a.Value < b.Value));
						TryOptimize<Number>(TargetMethodNames.OperatorLessThanOrEqualTo, (a, b, s) => new Instruction.Load.Boolean(s, a.Value <= b.Value));
						TryOptimize<bool>(TargetMethodNames.OperatorAnd, (a, b, s) => new Instruction.Load.Boolean(s, a && b));
						TryOptimize<bool>(TargetMethodNames.OperatorOr, (a, b, s) => new Instruction.Load.Boolean(s, a || b));

						void TryOptimize1<T>(string name, Func<T, int, Instruction?> optimize)
							=> CompileTimeComputing.TryOptimize<T>(name, scope, o, optimize, r => { didOpt = true; instructions[index] = r; });

						void TryOptimize<T>(string name, Func<T, T, int, Instruction?> optimize)
							=> CompileTimeComputing.TryOptimize<T>(name, scope, o, optimize, r => { didOpt = true; instructions[index] = r; });
					}
					break;

					case IClauseInstruction o:
					{
						Hunt(new Scope(scope), o.Clause, all);
					}
					break;

					default:
					{
						scope.Update(i);
					}
					break;
				}
			}

			return didOpt;
		}
	}
}
