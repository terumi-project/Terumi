using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Binder;

namespace Terumi.VarCode.Optimizer.Alpha
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

			// some notes:
			// - don't inline method 0
			// - TODO: check if an infinite loop will occur upon trying to inline the same method over and over
			//   ^ if this happens, we want to inline it only once.

			var tree = Translation.Trees[0];
			var didInline = false;

			// first: find a method that gets called

			for (var i = 0; i < tree.Code.Count; i++)
			{
				var instruction = tree.Code[i];

				switch (instruction)
				{
					case VarAssignment o:
					{
						if (o.Value is MethodCallVarExpression methodCall)
						{
							// we want to inline methodCall
							// since we're assigning a method call to a variable, we can assume that the method in question returns something
							// to check if we can inline, we want to make sure we only return at the end of the method
							// this is a limitation
							// TODO: allow method inlining for methods with a return statement not at the end

							if (!OneReturnAtEnd(Translation.Trees[methodCall.MethodId]))
							{
								// not eligable for inlining
								continue;
							}

							// great, we can inline the method
							// begin by taking the tree code and splicing it at the area of interest

							var treeCode = tree.Code;
							var newTreeCode = treeCode.Take(i).ToList(); // this will splice the list to right before the variable assignment

							// we will increment every variable in the inlined method by the 
							var varIncrement = tree.Counter;

							// inline it :o
							var preLength = newTreeCode.Count;
							var newCounter = InlineMethod(Translation.Trees[methodCall.MethodId].Code, newTreeCode, methodCall.ParameterVariables, varIncrement, o.VariableId);
							var postLength = newTreeCode.Count;

							tree.Counter = newCounter + 1;

							// add all the tree code back
							newTreeCode.AddRange(treeCode.Skip(i + 1));
							tree.Code = newTreeCode;

							i += postLength - preLength;

							didInline = true;
						}
					}
					break;

					case VarMethodCall o:
					{
						// we want to inline methodCall
						// since we're assigning a method call to a variable, we can assume that the method in question returns something
						// to check if we can inline, we want to make sure we only return at the end of the method
						// this is a limitation
						// TODO: allow method inlining for methods with a return statement not at the end

						var returnState = ReturnStateOf(Translation.Trees[o.MethodCallVarExpression.MethodId]);
						var hasReturn = o.VariableId != null;

						if ((hasReturn && returnState != ReturnState.InBody)
							|| (!hasReturn && returnState != ReturnState.Nowhere))
						{
							// not eligable for inlining
							continue;
						}

						// great, we can inline the method
						// begin by taking the tree code and splicing it at the area of interest

						var treeCode = tree.Code;
						var newTreeCode = treeCode.Take(i).ToList(); // this will splice the list to right before the variable assignment

						// we will increment every variable in the inlined method by the 
						var varIncrement = tree.Counter;

						// inline it :o
						var preLength = newTreeCode.Count;
						var newCounter = InlineMethod(Translation.Trees[o.MethodCallVarExpression.MethodId].Code, newTreeCode, o.MethodCallVarExpression.ParameterVariables, varIncrement, o.VariableId);
						var postLength = newTreeCode.Count;

						tree.Counter = newCounter + 1;

						// add all the tree code back
						newTreeCode.AddRange(treeCode.Skip(i + 1));
						tree.Code = newTreeCode;

						i += postLength - preLength;

						didInline = true;
					}
					break;

					case VarIf o:
					{
						// TODO: inline if statement
						// by cleaning up code
						// which is a huge mess
					}
					break;
				}
			}

			return didInline;
		}

		private int InlineMethod(List<VarInstruction> source, List<VarInstruction> destination, List<int> parameters, int varIncrement, int? returnVariable)
		{
			var max = 0;

			int TrySetMaxInc(int newValue)
			{
				if (newValue > max)
				{
					max = newValue;
				}

				return newValue;
			}

			foreach (var instruction in source)
			{
				switch (instruction)
				{
					case VarAssignment o:
					{
						destination.Add(new VarAssignment(TrySetMaxInc(o.VariableId + varIncrement), InlineMethod(o.Value, varIncrement, TrySetMaxInc)));
					}
					break;

					case VarReturn o:
					{
						if (returnVariable != null)
						{
							destination.Add(new VarAssignment((int)returnVariable, new ReferenceVarExpression(TrySetMaxInc(o.Id + varIncrement))));
						}
					}
					break;

					case VarMethodCall o:
					{
						var newId = o.VariableId == null
							? null
							: o.VariableId + varIncrement;

						destination.Add(new VarMethodCall(newId, (MethodCallVarExpression)InlineMethod(o.MethodCallVarExpression, varIncrement, TrySetMaxInc)));
					}
					break;

					case VarParameterAssignment o:
					{
						destination.Add(new VarAssignment(TrySetMaxInc(o.Id + varIncrement), new ReferenceVarExpression(parameters[o.ParameterId])));
					}
					break;

					case VarIf o:
					{
						var dest = new List<VarInstruction>();

						TrySetMaxInc(InlineMethod(o.TrueBody, dest, parameters, varIncrement, returnVariable));

						destination.Add(new VarIf(TrySetMaxInc(o.ComparisonVariable + varIncrement), dest));
					}
					break;
				}
			}

			return max;
		}

		private VarExpression InlineMethod(VarExpression expression, int varIncrement, Func<int, int> trySetMaxInc)
		{
			switch (expression)
			{
				case MethodCallVarExpression o:
				{
					return new MethodCallVarExpression(o.MethodId, o.ParameterVariables.Select(x => trySetMaxInc(x + varIncrement)).ToList());
				}

				case ReferenceVarExpression o:
				{
					return new ReferenceVarExpression(trySetMaxInc(o.VariableId + varIncrement));
				}
			}

			// probably a constant expression
			return expression;
		}

		private enum ReturnState
		{
			InBody,
			InChild,
			Nowhere
		}

		private bool OneReturnAtEnd(VarTree method)
		{
			return ReturnInBody(method.Code) == ReturnState.InBody;
		}

		private ReturnState ReturnStateOf(VarTree method) => ReturnInBody(method.Code);

		private ReturnState ReturnInBody(List<VarInstruction> instructions)
		{
			foreach (var i in instructions)
			{
				if (i is VarReturn) return ReturnState.InBody;

				if (i is VarIf varIf)
				{
					var results = ReturnInBody(varIf.TrueBody);

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
}
