using System;
using System.Collections.Generic;
using System.Linq;

namespace Terumi.VarCode.Optimizer.Omega
{
	public class InlineVariableReferences : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			var didInline = false;

			foreach (var function in store.Functions)
			{
				didInline = Run(function.Instructions) || didInline;
			}

			didInline = Run(store.Instructions) || didInline;

			return didInline;
		}

		public bool Run(List<VarInstruction> instructions)
		{
			// if we assign a value to a variable, we can replace references to that variable with a value
			// note that this may not work always for things like

			/*
			 * $a = "test"
			 * if $parameter {
			 *		$a = "ok"
			 * } else {
			 *		$a = "boomer"
			 * }
			 * @println($a)
			 */

			// so when we enter if statements and modify existing values, we want to remove them from the map
			// as their modification is indetermineable

			var valueMap = new Dictionary<VarCodeId, VarExpression>();
			return Run(valueMap, instructions, false);
		}

		private bool Run(Dictionary<VarCodeId, VarExpression> valueMap, List<VarInstruction> instructions, bool delete = true)
		{
			var didModify = false;

			foreach (var instruction in instructions)
			{
				switch (instruction)
				{
					case VarAssignment o:
					{
						Update(valueMap, delete, o.VariableId, o.Value);
					}
					break;

					case VarReturn o:
					{
						o.Value = Inline(o.Value);
					}
					break;

					case VarMethodCall o:
					{
						o.MethodCallVarExpression = (MethodCallVarExpression)Inline(o.MethodCallVarExpression);
					}
					break;

					case VarIf o:
					{
						o.ComparisonExpression = Inline(o.ComparisonExpression);
						didModify = Run(valueMap, o.TrueBody) || didModify;
					}
					break;

					default: throw new NotImplementedException();
				}
			}

			return didModify;

			VarExpression Inline(VarExpression target) => InlineVariableReferences.Inline(valueMap, target, () => didModify = true);
		}

		private static VarExpression Inline(Dictionary<VarCodeId, VarExpression> valueMap, VarExpression target, Action enable)
			=> target switch
			{
				IConstantVarExpression o => target,
				MethodCallVarExpression o => new MethodCallVarExpression(o.MethodId, o.Parameters.Select(x => Inline(valueMap, x, enable)).ToList()),
				ReferenceVarExpression o => Inline(valueMap, enable, o),
				ParameterReferenceVarExpression o => o,
				_ => throw new NotImplementedException(),
			};

		private static VarExpression Inline
		(
			Dictionary<VarCodeId, VarExpression> valueMap,
			Action enable,
			ReferenceVarExpression o
		)
		{
			if (valueMap.TryGetValue(o.VariableId, out var replExpr)
				&& replExpr != null)
			{
				enable();
				return replExpr;
			}

			return o;
		}

		private void Update(Dictionary<VarCodeId, VarExpression> valueMap, bool delete, VarCodeId id, VarExpression expression)
		{
			if (valueMap.TryGetValue(id, out var value))
			{
				valueMap[id] = delete ? null : expression;
			}
			else
			{
				valueMap[id] = expression;
			}
		}


	}
}