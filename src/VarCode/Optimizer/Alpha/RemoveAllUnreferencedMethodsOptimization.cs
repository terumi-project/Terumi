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
			var references = AllReferences(entry);

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

		private List<VarCodeId> AllReferences(VarCodeStructure investigate)
		{
			var references = new List<VarCodeId>();

			foreach (var loc in investigate.Tree.Code)
			{
				var expression = loc.TryExpression();

				if (expression == default)
				{
					continue;
				}

				if (expression is MethodCallVarExpression methodCall
					&& !references.Contains(methodCall.MethodId))
				{
					references.Add(methodCall.MethodId);
				}
			}

			return references;
		}
	}
}
