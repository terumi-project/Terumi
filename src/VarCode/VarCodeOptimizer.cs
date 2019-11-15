using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode
{
	public class VarCodeOptimizer
	{
		public VarCodeOptimizer(VarCodeTranslation translation) => Translation = translation;

		public VarCodeTranslation Translation { get; }

		public void Optimize()
		{
			bool did1, did2;

			do
			{
				did1 = RemoveAllUnreferencedMethods();
				did2 = InlineMethods();
			}
			while (did1 || did2);
		}

		public bool RemoveAllUnreferencedMethods()
		{
			var references = new List<int> { 0 };
			var exploredReferences = new List<int>();

			// while not all the explored references are listed in the references
			while (!references.All(x => exploredReferences.Contains(x)))
			{
				var explore = references.Where(x => !exploredReferences.Contains(x)).ToArray();

				foreach (var i in explore)
				{
					FindReferences(references, i);
					exploredReferences.Add(i);
				}

				references = references.Distinct().ToList();
			}

			// now we have every reference
			// delete any non referenced methods

			var deleted = false;

			for (var i = 0; i < Translation.Trees.Count; i++)
			{
				if (!references.Contains(i))
				{
					var tree = Translation.Trees[i];

					if (tree == null)
					{
						continue;
					}

					Translation.Trees[i] = null;
					deleted = true;
				}
			}

			return deleted;
		}

		private void FindReferences(List<int> referenceList, int method)
		{
			var tree = Translation.Trees[method];

			// TODO: investigate
			if (tree == null)
			{
				return;
			}

			foreach (var instruction in tree.Code)
			{
				MethodReferenceOf(referenceList, instruction);
			}
		}

		private void MethodReferenceOf(List<int> referenceList, VarInstruction instruction)
		{
			switch (instruction)
			{
				case VarAssignment i:
				{
					if (i.Value is MethodCallVarExpression expression)
					{
						referenceList.Add(expression.MethodId);
					}
				}
				break;

				case VarMethodCall i:
				{
					referenceList.Add(i.MethodCallVarExpression.MethodId);
				}
				break;

				case VarIf i:
				{
					foreach (var statement in i.TrueBody)
					{
						MethodReferenceOf(referenceList, statement);
					}
				}
				break;
			}
		}

		public bool InlineMethods()
		{
			// we only want to optimize method 0
			// method 0 is the entry point
			// so we'll look for all method calls in method 0
			// and then, if after determining if it's inlineable that it's inlineable, inline it.

			return false;
		}
	}
}
