using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.Binder
{
	public class TypeSimilarity
	{
		public static bool MayBeUsedAs(IType mustBe, IType tryingToPoseAs)
		{
			var similarityComparer = new TypeSimilarity();
			var confirmedStack = new List<(IType, IType)>();
			var compareStack = new List<(IType, IType)>() { (mustBe, tryingToPoseAs) };

			while (compareStack.Count > 0)
			{
				var top = compareStack[0];
				compareStack.RemoveAt(0);

				if (confirmedStack.Contains(top))
				{
					continue;
				}

				var cmp = similarityComparer.Compare(top.Item1, top.Item2);

				if (cmp == CompareDecision.Failure)
				{
					return false;
				}
				else if (cmp == CompareDecision.Waiting)
				{
					foreach (var item in similarityComparer.NeedsToBeSimilar)
					{
						if (!confirmedStack.Contains(item))
						{
							compareStack.Add(item);
						}
					}
				}

				confirmedStack.Add(top);
			}

			// as long as there's no explicit "NO" errors, this means we're most likely good to go
			// for the most part this should catch everything

			return true;
		}

		// https://thedailywtf.com/articles/What_Is_Truth_0x3f_
		// :^)

		/// <summary>
		/// Used when determining if two types are equivalent. This kind
		/// of operation is pretty cheap.
		/// </summary>
		private enum FlattenedComparison
		{
			/// <summary>
			/// The two types are definitely equal.
			/// </summary>
			True,

			/// <summary>
			/// The two types are definitely not equal.
			/// </summary>
			False,

			/// <summary>
			/// If both types aren't builtin types, this is returned. This
			/// signifies that a significant amount of comparison needs to be
			/// done to obtain a usuable result.
			/// </summary>
			FileNotFound,
		}

		private static FlattenedComparison FlattenedCompare(IType mustBe, IType tryingToPoseAs)
		{
			// simple short circuit
			if (mustBe == tryingToPoseAs)
			{
				return FlattenedComparison.True;
			}

			if (BuiltinType.IsBuiltinType(mustBe))
			{
				// no matter what the other type is, if they didn't equal eachother
				// it's over

				return FlattenedComparison.False;
			}
			else
			{
				if (!BuiltinType.IsBuiltinType(tryingToPoseAs))
				{
					return FlattenedComparison.FileNotFound;
				}

				return FlattenedComparison.False;
			}
		}

		private static bool TryField(List<Field> fields, string name, out Field result)
		{
			foreach (var f in fields)
			{
				if (f.Name == name)
				{
					result = f;
					return true;
				}
			}

			result = default;
			return false;
		}

		private static bool TryMethods(List<IMethod> methods, string name, out List<IMethod> result)
		{
			result = new List<IMethod>();

			foreach (var m in methods)
			{
				if (m.Name == name)
				{
					result.Add(m);
				}
			}

			return result.Count > 0;
		}

		private enum CompareDecision
		{
			Waiting,
			Success,
			Failure,
		}

		// List<(A, B)> where A belongs to _mustBe and B belongs to tryingToPoseAs
		public readonly List<(IType, IType)> NeedsToBeSimilar = new List<(IType, IType)>();

		private CompareDecision Compare(IType mustBe, IType tryingToPoseAs)
		{
			if (mustBe == tryingToPoseAs) return CompareDecision.Success;

			if (BuiltinType.IsBuiltinType(mustBe))
				return mustBe == tryingToPoseAs ? CompareDecision.Success : CompareDecision.Failure;

			// check if method names & fields are similar
			foreach (var field in mustBe.Fields)
			{
				if (!TryField(tryingToPoseAs.Fields, field.Name, out var targetField))
				{
					return CompareDecision.Failure;
				}

				var cmp = FlattenedCompare(field.Type, targetField.Type);

				if (cmp == FlattenedComparison.False)
				{
					return CompareDecision.Failure;
				}
				else if (cmp == FlattenedComparison.FileNotFound)
				{
					NeedsToBeSimilar.Add((field.Type, targetField.Type));
				}
			}

			var deeperMethod = new List<(IMethod, IMethod)>();

			foreach (var method in mustBe.Methods.Where(method => !method.IsConstructor))
			{
				if (!TryMethods(tryingToPoseAs.Methods, method.Name, out var targetMethods))
				{
					return CompareDecision.Failure;
				}

				if (!TryMethod())
				{
					return CompareDecision.Failure;
				}

				bool TryMethod()
				{
					foreach (var targetMethod in targetMethods)
					{
						var needs = new List<(IType, IType)>();

						if (DoCmp() == CompareDecision.Success)
						{
							return true;
						}

						CompareDecision DoCmp()
						{
							if (method.Parameters.Count != targetMethod.Parameters.Count)
							{
								return CompareDecision.Failure;
							}

							var methodCmps = method.Parameters
								.Select(x => x.Type)
								.Prepend(method.ReturnType);

							var targetCmps = targetMethod.Parameters
								.Select(x => x.Type)
								.Prepend(method.ReturnType);

							var cmps = methodCmps.Zip(targetCmps).Select((a) => (FlattenedCompare(a.First, a.Second), a));

							foreach (var (comparison, (methodCmp, targetCmp)) in cmps)
							{
								if (comparison == FlattenedComparison.False)
								{
									return CompareDecision.Failure;
								}
								else if (comparison == FlattenedComparison.FileNotFound)
								{
									needs.Add((methodCmp, targetCmp));
								}
							}

							return CompareDecision.Success;
						}
					}

					return false;
				}
			}

			// done all comparisons

			if (NeedsToBeSimilar.Count == 0)
			{
				return CompareDecision.Success;
			}

			// need to further investigate

			return CompareDecision.Waiting;
		}
	}
}