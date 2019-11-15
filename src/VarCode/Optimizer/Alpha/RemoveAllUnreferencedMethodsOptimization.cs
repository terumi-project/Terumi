using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimizer.Alpha
{
	public class RemoveAllUnreferencedMethodsOptimization : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			var entry = store.Entrypoint;
			var references = AllReferences(entry, entry.Tree.Code);

			var deleteStructures = new List<VarCodeStructure>();

			foreach (var structure in store.Structures)
			{
				if (!references.Contains(structure.Id))
				{
					deleteStructures.Add(structure);
				}
			}

			foreach (var structure in deleteStructures)
			{
				store.TryRemove(structure);
			}

			return deleteStructures.Count > 0;
		}

		private List<VarCodeId> AllReferences(VarCodeStructure operate, List<VarInstruction> instructions)
		{
			var references = new List<VarCodeId>();

			foreach (var instruction in instructions)
			{
				if (instruction is VarIf varIf)
				{
					foreach (var innerReference in AllReferences(operate, varIf.TrueBody))
					{
						if (!references.Contains(innerReference))
						{
							references.Add(innerReference);
						}
					}
				}

				if (instruction is VarMethodCall varMethodCall)
				{
					if (!references.Contains(varMethodCall.MethodCallVarExpression.MethodId))
					{
						references.Add(varMethodCall.MethodCallVarExpression.MethodId);
					}

					var method = operate.Store.GetStructure(varMethodCall.MethodCallVarExpression.MethodId);

					if (method == null)
					{
						continue;
					}

					foreach (var innerReference in AllReferences(operate, method.Tree.Code))
					{
						if (!references.Contains(innerReference))
						{
							references.Add(innerReference);
						}
					}
				}

				var expression = instruction.TryExpression();

				if (expression == default)
				{
					continue;
				}

				if (expression is MethodCallVarExpression methodCall)
				{
					if (!references.Contains(methodCall.MethodId))
					{
						references.Add(methodCall.MethodId);
					}

					var method = operate.Store.GetStructure(methodCall.MethodId);

					if (method == null)
					{
						continue;
					}

					foreach (var innerReference in AllReferences(operate, method.Tree.Code))
					{
						if (!references.Contains(innerReference))
						{
							references.Add(innerReference);
						}
					}
				}
			}

			return references;
		}
	}
}
