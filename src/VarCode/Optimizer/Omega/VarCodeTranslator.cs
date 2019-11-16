using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.VarCode.Optimizer;

using AlphaStore = Terumi.VarCode.Optimizer.Alpha.VarCodeStore;
using AlphaInstruction = Terumi.VarCode.Optimizer.Alpha.VarInstruction;
using AlphaAssignment = Terumi.VarCode.Optimizer.Alpha.VarAssignment;
using AlphaReturn = Terumi.VarCode.Optimizer.Alpha.VarReturn;
using AlphaMethodCall = Terumi.VarCode.Optimizer.Alpha.VarMethodCall;
using AlphaParameterAssignment = Terumi.VarCode.Optimizer.Alpha.VarParameterAssignment;
using AlphaIf = Terumi.VarCode.Optimizer.Alpha.VarIf;

using AlphaExpression = Terumi.VarCode.Optimizer.Alpha.VarExpression;
using AlphaMethodCallExpression = Terumi.VarCode.Optimizer.Alpha.MethodCallVarExpression;
using AlphaReferenceExpression = Terumi.VarCode.Optimizer.Alpha.ReferenceVarExpression;

using OmegaStore = Terumi.VarCode.Optimizer.Omega.VarCodeStore;
using OmegaInstruction = Terumi.VarCode.Optimizer.Omega.VarInstruction;
using OmegaAssignment = Terumi.VarCode.Optimizer.Omega.VarAssignment;
using OmegaReturn = Terumi.VarCode.Optimizer.Omega.VarReturn;
using OmegaMethodCall = Terumi.VarCode.Optimizer.Omega.VarMethodCall;
using OmegaIf = Terumi.VarCode.Optimizer.Omega.VarIf;

using OmegaExpression = Terumi.VarCode.Optimizer.Omega.VarExpression;
using OmegaMethodCallExpression = Terumi.VarCode.Optimizer.Omega.MethodCallVarExpression;
using OmegaReferenceExpression = Terumi.VarCode.Optimizer.Omega.ReferenceVarExpression;
using OmegaParameterReferenceExpression = Terumi.VarCode.Optimizer.Omega.ParameterReferenceVarExpression;
using System.Numerics;

namespace Terumi.VarCode.Optimizer.Omega
{
	public static class VarCodeTranslator
	{
		public static OmegaStore Translate(AlphaStore store) => Translate(store, false);

		private static OmegaStore Translate(AlphaStore aStore, bool _)
		{
			var oStore = new OmegaStore();

			oStore.Instructions = Translate(aStore.Entrypoint.Tree.Code);

			foreach (var structure in aStore.Structures.Where(x => x != aStore.Entrypoint))
			{
				oStore.Functions.Add(new Structure(structure.Id, Translate(structure.Tree.Code), structure.MethodBind.Parameters.Count));
			}

			foreach (var (methodId, compilerMethod) in aStore.CompilerMethods)
			{
				oStore.CompilerMethods.Add(methodId, compilerMethod);
			}

			return oStore;
		}

		private static List<OmegaInstruction> Translate(List<AlphaInstruction> instructions)
		{
			var dest = new List<OmegaInstruction>();

			foreach (var instruction in instructions)
			{
				switch (instruction)
				{
					case AlphaAssignment o:
					{
						dest.Add(new OmegaAssignment(o.VariableId, Translate(o.Value)));
					}
					break;

					case AlphaReturn o:
					{
						dest.Add(new OmegaReturn(new OmegaReferenceExpression(o.Id)));
					}
					break;

					case AlphaMethodCall o:
					{
						var methodCallTranslation = (OmegaMethodCallExpression)Translate(o.MethodCallVarExpression);

						if (o.VariableId == null)
						{
							dest.Add(new OmegaMethodCall(methodCallTranslation));
						}
						else
						{
							dest.Add(new OmegaAssignment((VarCodeId)o.VariableId, methodCallTranslation));
						}
					}
					break;

					case AlphaParameterAssignment o:
					{
						dest.Add(new OmegaAssignment(o.Id, new OmegaParameterReferenceExpression(o.ParameterId)));
					}
					break;

					case AlphaIf o:
					{
						dest.Add(new OmegaIf(new OmegaReferenceExpression(o.ComparisonVariable), Translate(o.TrueBody)));
					}
					break;

					default: throw new NotImplementedException();
				}
			}

			return dest;
		}

		private static OmegaExpression Translate(AlphaExpression expression)
		{
			switch (expression)
			{
				case AlphaMethodCallExpression o:
				{
					var parameters = o.ParameterVariables.Select(x => new OmegaReferenceExpression(x)).Cast<OmegaExpression>().ToList();
					return new OmegaMethodCallExpression(o.MethodId, parameters);
				}

				case AlphaReferenceExpression o:
				{
					return new OmegaReferenceExpression(o.VariableId);
				}

				case IConstantVarExpression o:
				{
					return o.Value switch
					{
						string p => new ConstantVarExpression<string>(p),
						BigInteger p => new ConstantVarExpression<BigInteger>(p),
						bool p => new ConstantVarExpression<bool>(p),
						_ => throw new NotImplementedException()
					};
				}

				default: throw new NotImplementedException();
			}
		}
	}
}