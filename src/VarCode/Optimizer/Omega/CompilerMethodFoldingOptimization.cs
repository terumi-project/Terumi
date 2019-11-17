using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimizer.Omega
{
	public class CompilerMethodFoldingOptimization : IOptimization
	{
		public bool Run(VarCodeStore store)
		{
			var didFold = false;

			foreach (var function in store.Functions)
			{
				didFold = Run(store, function.Instructions) || didFold;
			}

			return Run(store, store.Instructions) || didFold;
		}

		public static bool Run(VarCodeStore store, List<VarInstruction> instructions)
		{
			var didFold = false;

			foreach (var instruction in instructions)
			{
				switch (instruction)
				{
					case VarAssignment o:
					{
						didFold = Run(o.Value, r => o.Value = r) || didFold;
					}
					break;

					case VarReturn o:
					{
						didFold = Run(o.Value, r => o.Value = r) || didFold;
					}
					break;

					// TODO: get this working - for now we'll ignore it
					/*
					case VarMethodCall o:
					{
						didFold = Run(o.MethodCallVarExpression, r => o.MethodCallVarExpression = r) || didFold;
					}
					break;
					*/

					case VarIf o:
					{
						didFold = Run(o.ComparisonExpression, r => o.ComparisonExpression = r) || didFold;
					}
					break;
				}
			}

			return didFold;

			bool Run(VarExpression a, Action<VarExpression> b) => CompilerMethodFoldingOptimization.Run(store, a, b);
		}

		public static bool Run(VarCodeStore store, VarExpression expression, Action<VarExpression> replace)
		{
			switch (expression)
			{
				case IConstantVarExpression _: return false;
				case ReferenceVarExpression _: return false;
				case ParameterReferenceVarExpression _: return false;

				case MethodCallVarExpression o:
				{
					if (store.CompilerMethods.TryGetValue(o.MethodId, out var compilerMethod))
					{
						var optimized = compilerMethod.Optimize(o.Parameters);

						if (optimized != null)
						{
							replace(optimized);
							return true;
						}
					}

					var didFold = false;

					for (var i = 0; i < o.Parameters.Count; i++)
					{
						didFold = Run(store, o.Parameters[i], r => o.Parameters[i] = r) || didFold;
					}

					return didFold;
				}
			}

			throw new NotImplementedException();
		}
	}
}
