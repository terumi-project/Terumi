using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode.Optimizer.Alpha
{
	public class RemoveAllUnreferencedMethodsOptimization : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			var entry = store.Entrypoint;
			var references = AllReferences(entry, entry.Tree.Code);
			references.Add(entry.Id); // we don't want the entry itself to die

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
			=> AllReferences(new List<VarCodeId>(), operate.Store, operate, instructions);

		private List<VarCodeId> AllReferences(List<VarCodeId> stack, VarCodeStore store, VarCodeStructure smethod, List<VarInstruction> instructions)
		{
			if (stack.Count(x => x == smethod.Id) >= 10) // we don't want to be more than 10 times recursive @ compile time
			{
				// potential recursion
				return new List<VarCodeId>();
			}

			var references = new List<VarCodeId>();

			foreach (var instruction in instructions)
			{
				if (instruction is VarIf varIf)
				{
					foreach (var innerReference in AllReferences(stack, store, smethod, varIf.TrueBody))
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

					var method = store.GetStructure(varMethodCall.MethodCallVarExpression.MethodId);

					if (method == null)
					{
						continue;
					}

					stack.Add(method.Id);
					foreach (var innerReference in AllReferences(stack, store, method, method.Tree.Code))
					{
						if (!references.Contains(innerReference))
						{
							references.Add(innerReference);
						}
					}
					stack.Remove(method.Id);
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

					var method = store.GetStructure(methodCall.MethodId);

					if (method == null)
					{
						continue;
					}

					stack.Add(method.Id);
					foreach (var innerReference in AllReferences(stack, store, method, method.Tree.Code))
					{
						if (!references.Contains(innerReference))
						{
							references.Add(innerReference);
						}
					}
					stack.Remove(method.Id);
				}
			}

			return references;
		}
	}
}
