using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode.Optimizer.Alpha
{
	public class MethodInliningOptimization : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			// TODO: inline other methods who they themselves can't be inlined but use methods that can
			return Run(store.Entrypoint);
		}

		private bool Run(VarCodeStructure operate) => Run(operate, ref operate.Tree.Counter, operate.Tree.Code);

		private bool Run(VarCodeStructure operate, ref VarCodeId counter, List<VarInstruction> instructions)
		{
			var couldInlineOne = false;

			for (var i = 0; i < instructions.Count; i++)
			{
				var instruction = instructions[i];
				switch (instruction)
				{
					case VarAssignment o:
					{
						if (!(o.Value is MethodCallVarExpression methodCall))
						{
							continue;
						}

						couldInlineOne = TryInline(operate, ref counter, instructions, i, methodCall, o.VariableId) || couldInlineOne;
					}
					break;

					case VarMethodCall o:
					{
						couldInlineOne = TryInline(operate, ref counter, instructions, i, o.MethodCallVarExpression, o.VariableId) || couldInlineOne;
					}
					break;

					case VarIf o:
					{
						couldInlineOne = Run(operate, ref counter, o.TrueBody) || couldInlineOne;
					}
					break;
				}
			}

			return couldInlineOne;
		}

		private bool TryInline(VarCodeStructure operate, ref VarCodeId counter, List<VarInstruction> instructions, int i, MethodCallVarExpression call, VarCodeId? resultAssignment)
		{
			// before we determine if we can inline this method, we must determine if the method
			// has a return statement somewhere in itself.

			// if it returns anywhere except at the end, this will prevent us from inlining it, because
			// to inline it, we'd need to generate code to exhibit the same 'exit early' effect

			// TODO: inline methods with returns somewhere not at the end

			var rawStructure = operate.Store.GetStructure(call.MethodId);
			if (rawStructure == null) return false; // can't inline compiler method calls

			var inlining = rawStructure;
			var returnState = ReturnStateOf(inlining.Tree.Code);

			var canInline =
				returnState == ReturnState.InBody // we can inline if there's a return statement in the body
				|| (returnState == ReturnState.Nowhere && resultAssignment == null); // or if there's a return statement & we don't need a result

			if (!canInline)
			{
				return false;
			}

			// TODO: check if the method references some kind of loop such that we can't inline it.

			// now we have to begin the gruesome process of merging the two code trees
			var merger = new CodeTreeMerger(counter, instructions, operate, i, call, resultAssignment);

			merger.Merge();

			// if the inlined method caused us to define variables like 17 when the counter was only 12,
			// we want to update the counter to 18 so that new variable declarations and future inlings
			// use only unused variables
			counter = merger.HighestSetVariable + 1;

			return true;
		}

		private enum ReturnState
		{
			Nowhere,
			InBody,
			InChild
		}

		private static ReturnState ReturnStateOf(List<VarInstruction> instructions)
		{
			foreach (var i in instructions)
			{
				if (i is VarReturn) return ReturnState.InBody;

				if (i is VarIf varIf)
				{
					var results = ReturnStateOf(varIf.TrueBody);

					if (results == ReturnState.InChild
						|| results == ReturnState.InBody)
					{
						return ReturnState.InChild;
					}
				}
			}

			return ReturnState.Nowhere;
		}
	}

	public class CodeTreeMerger
	{
		private readonly VarCodeId _variableAppend;
		private readonly VarCodeStructure _operate;
		private readonly int _i;
		private readonly MethodCallVarExpression _call;
		private readonly VarCodeId? _resultAssignment;
		private List<VarInstruction> _instructions;

		public CodeTreeMerger(VarCodeId variableAppend, List<VarInstruction> instructions, VarCodeStructure operate, int i, MethodCallVarExpression call, VarCodeId? resultAssignment)
		{
			HighestSetVariable = default;

			_variableAppend = variableAppend;
			_operate = operate;
			_i = i;
			_call = call;
			_resultAssignment = resultAssignment;
			_instructions = instructions;
		}

		public VarCodeId HighestSetVariable { get; set; }

		public void Merge()
		{
			// first, prune the tree up until right before the method call
			var treeCode = _instructions;
			var pruned = treeCode.Take(_i).ToList();

			// do inlining

			// might want to be a ctor parameter, but meh
			var inliningTarget = _operate.Store.GetStructure(_call.MethodId);

			foreach (var instruction in inliningTarget.Tree.Code)
			{
				InlineInstruction(instruction, pruned);
			}

			// now we want to stich back on the stuff we pruned
			pruned.AddRange(treeCode.Skip(_i + 1));

			// TODO: better way to set instructions
			_instructions.Clear();
			_instructions.AddRange(pruned);
		}

		private bool InlineInstruction(VarInstruction instruction, List<VarInstruction> target)
		{
			var parameters = _call.ParameterVariables;

			switch (instruction)
			{
				case VarAssignment o: target.Add(new VarAssignment(Add(o.VariableId), PassInlineExpression(o.Value))); break;
				case VarParameterAssignment o: target.Add(new VarAssignment(Add(o.Id), new ReferenceVarExpression(parameters[o.ParameterId]))); break;

				case VarMethodCall o:
				{
					var newId = o.VariableId == null
						? null
						: (VarCodeId?)Add((VarCodeId)o.VariableId);

					target.Add(new VarMethodCall(newId, (MethodCallVarExpression)PassInlineExpression(o.MethodCallVarExpression)));
				}
				break;

				case VarIf o:
				{
					var dest = new List<VarInstruction>();

					foreach (var i in o.TrueBody)
					{
						InlineInstruction(i, dest);
					}

					target.Add(new VarIf(Add(o.ComparisonVariable), dest));
				}
				break;

				case VarReturn o:
				{
					if (_resultAssignment == null)
					{
						return true;
					}

					target.Add(new VarAssignment((VarCodeId)_resultAssignment, new ReferenceVarExpression(Add(o.Id))));

					return true;
				}
			}

			return false;

			// we do this incase InlineExpression happens to need variables from InlineInstruction
			// so we can easily pass them in here

			VarExpression PassInlineExpression(VarExpression expression)
			{
				return InlineExpression(expression);
			}
		}

		private VarExpression InlineExpression(VarExpression varExpression)
		{
			switch (varExpression)
			{
				case MethodCallVarExpression o:
				{
					return new MethodCallVarExpression(o.MethodId, o.ParameterVariables.Select(Add).ToList());
				}

				case ReferenceVarExpression o:
				{
					return new ReferenceVarExpression(Add(o.VariableId));
				}
			}

			// probably a constant expression
			return varExpression;
		}

		// we add to variable ids since we can *guarentee* they start from 0
		// and go up to n, so we set the highest variable to n + _variableAppend
		// so that the caller can update the counter so that the counter can guarentee
		// that only new variables will be declared
		private VarCodeId Add(VarCodeId initial)
		{
			var @new = initial + _variableAppend;

			if (@new > HighestSetVariable)
			{
				HighestSetVariable = @new;
			}

			return @new;
		}
	}
}
