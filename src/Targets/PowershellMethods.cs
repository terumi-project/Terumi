using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Terumi.Binder;
using Terumi.VarCode;

namespace Terumi.Targets
{
	public class PowershellTarget : ICompilerTarget
	{
		public CompilerMethod? Match(string name, params IType[] types)
		{
			switch (name)
			{
				// TODO: supply code
				case TargetMethodNames.TargetName: return ReturnMethod(BuiltinType.String, _ => "powershell");

				case TargetMethodNames.IsSupported: return ReturnMethod(BuiltinType.Boolean, _ => "$TRUE"); // TODO: exauhstive
				case TargetMethodNames.Println: return ReturnMethod(BuiltinType.Void, a => $"Write-Host {a[0]}");
				case TargetMethodNames.Command: return ReturnMethod(BuiltinType.Void, a => $"iex \"& {a[0]}\"");

				case TargetMethodNames.OperatorNot: return ReturnMethod(BuiltinType.Boolean, a => $"-Not {a[0]}");

				case TargetMethodNames.OperatorEqualTo: return ReturnMethod(BuiltinType.Boolean, a => $"{a[0]} -eq {a[1]}");
				case TargetMethodNames.OperatorNotEqualTo: return ReturnMethod(BuiltinType.Boolean, a => $"{a[0]} -ne {a[1]}");

				case TargetMethodNames.OperatorLessThan: return ReturnMethod(BuiltinType.Boolean, a => $"{a[0]} -lt {a[1]}");
				case TargetMethodNames.OperatorGreaterThan: return ReturnMethod(BuiltinType.Boolean, a => $"{a[0]} -gt {a[1]}");
				case TargetMethodNames.OperatorLessThanOrEqualTo: return ReturnMethod(BuiltinType.Boolean, a => $"{a[0]} -le {a[1]}");
				case TargetMethodNames.OperatorGreaterThanOrEqualTo: return ReturnMethod(BuiltinType.Boolean, a => $"{a[0]} -ge {a[1]}");

				// TODO: verify that both operands are the same
				case TargetMethodNames.OperatorAdd: return ReturnMethod(types[0], a => $"{a[0]} + {a[1]}");
				case TargetMethodNames.OperatorSubtract: return ReturnMethod(types[0], a => $"{a[0]} - {a[1]}");
				case TargetMethodNames.OperatorMultiply: return ReturnMethod(types[0], a => $"{a[0]} * {a[1]}");
				case TargetMethodNames.OperatorDivide: return ReturnMethod(types[0], a => $"{a[0]} / {a[1]}");
				case TargetMethodNames.OperatorExponent: return ReturnMethod(types[0], a => $"[Math]::Pow({a[0]}, {a[1]})");
			}

			throw new NotImplementedException();
			CompilerMethod ReturnMethod(IType returnType, Func<List<string>, string> codegen) => new CompilerMethod(returnType, name, Match(types))
			{
				CodeGen = codegen
			};
		}

		private List<MethodParameter> Match(IType[] arguments)
			=> arguments.Select((x, i) => new MethodParameter(x, $"p{i}")).ToList();

		public void Write(IndentedTextWriter writer, List<VarCode.Method> methods)
		{
			int id = 0;
			foreach (var method in methods) method.Id = id++;

			writer.WriteLine($"$_gc = 0");

			foreach (var method in methods)
			{
				writer.Write($"function {GetName(method.Id)}");

				if (method.Parameters.Count > 0)
				{
					writer.Write($"(${GetName(0)}");

					for (var i = 1; i < method.Parameters.Count; i++)
					{
						writer.Write(", ");
						writer.Write('$');
						writer.Write(GetName(i));
					}

					writer.Write(')');
				}
				else
				{
					writer.Write("()");
				}

				writer.WriteLine('{');
				writer.Indent++;

				Write(writer, method, method.Parameters.Count);

				writer.Indent--;
				writer.Write('}');
			}
		}

		public void Write(IndentedTextWriter writer, VarCode.Method method, int offset)
			=> Write(writer, method.Code, offset);

		public void Write(IndentedTextWriter writer, List<Instruction> body, int offset)
		{
			foreach (var i in body)
			{
				Write(writer, i, offset);
			}
		}

		public void Write(IndentedTextWriter writer, Instruction instruction, int offset)
		{
			switch (instruction)
			{
				case Instruction.Load.String o:
				{
					writer.WriteLine($"${GetName(o.Store)} = \"{o.Value}\"");
				}
				break;

				case Instruction.Load.Parameter o:
				{
					writer.WriteLine($"${GetName(o.Store)} = ${PowershellTarget.GetName(o.ParameterNumber)}");
				}
				break;

				case Instruction.Load.Boolean o:
				{
					writer.WriteLine($"${GetName(o.Store)} = ${(o.Value ? "TRUE" : "FALSE")}");
				}
				break;

				case Instruction.Load.Number o:
				{
					writer.WriteLine($"${GetName(o.Store)} = {o.Value.Value.ToString()}");
				}
				break;

				case Instruction.Assign o:
				{
					writer.WriteLine($"${GetName(o.Store)} = ${GetName(o.Value)}");
				}
				break;

				case Instruction.New o:
				{
					writer.WriteLine($"${GetName(o.StoreId)} = $_gc++");
				}
				break;

				// TODO: figure this out lol
				case Instruction.SetField o:
				{
					writer.WriteLine($"Set-Variable -Scope 'Global' -Name \"${GetName(o.VariableId)}.{PowershellTarget.GetName(o.FieldId)}\" -Value ${GetName(o.ValueId)}");
				}
				break;

				case Instruction.GetField o:
				{
					writer.WriteLine($"${GetName(o.StoreId)} = Get-Variable -Scope 'Global' -ValueOnly -Name \"${GetName(o.VariableId)}.{PowershellTarget.GetName(o.FieldId)}\"");
				}
				break;

				case Instruction.Call o:
				{
					var args = o.Arguments.Count == 0 ? "" : o.Arguments.Select(x => "$" + GetName(x)).Aggregate((a, b) => $"{a} {b}");

					if (o.Store == Instruction.Nowhere)
					{
						writer.WriteLine($"{PowershellTarget.GetName(o.Method.Id)} {args}");
					}
					else
					{
						writer.WriteLine($"${GetName(o.Store)} = {PowershellTarget.GetName(o.Method.Id)} {args}");
					}
				}
				break;

				case Instruction.CompilerCall o:
				{
					var toNames = o.Arguments.Select(x => "$" + GetName(x)).ToList();

					if (o.Store == Instruction.Nowhere)
					{
						writer.WriteLine($"{o.CompilerMethod.CodeGen(toNames)}");
					}
					else
					{
						writer.WriteLine($"${GetName(o.Store)} = {o.CompilerMethod.CodeGen(toNames)}");
					}
				}
				break;

				case Instruction.Return o:
				{
					writer.WriteLine($"return ${GetName(o.ValueId)}");
				}
				break;

				case Instruction.If o:
				{
					writer.WriteLine($"if (${GetName(o.Variable)}) {{");

					writer.Indent++;
					Write(writer, o.Clause, offset);
					writer.Indent--;

					writer.WriteLine('}');
				}
				break;

				case Instruction.While o:
				{
					writer.WriteLine($"while (${GetName(o.Comparison)}) {{");

					writer.Indent++;
					Write(writer, o.Clause, offset);
					writer.Indent--;

					writer.WriteLine('}');
				}
				break;

				default: throw new NotImplementedException();
			}

			string GetName(int id) => PowershellTarget.GetName(id + offset);
		}

		private static string GetName(int id)
		{
			return new string(id.ToString().Select(ToChar).ToArray());

			static char ToChar(char i)
				=> (i - '0') switch
				{
					0 => 'a',
					1 => 'b',
					2 => 'c',
					3 => 'd',
					4 => 'e',
					5 => 'f',
					6 => 'g',
					7 => 'h',
					8 => 'i',
					9 => 'j'
				};
		}

		/*
		public CompilerMethod? MatchMethod(string name, params IType[] parameters)
		{
			if (name == "println" && parameters.Length > 0)
			{
				return new CompilerMethod
				{
					Name = name,
					Parameters = parameters.Select((x, i) => new ParameterBind { Name = $"p{i}", Type = x }).ToList(),
					ReturnType = CompilerDefined.Void,
					Generate = strs =>
					{
						var strb = new StringBuilder("Write-Host ");

						if (strs.Count == 1)
						{
							strb.Append(strs[0]);
						}
						else
						{
							strb.Append('"');

							foreach (var str in strs)
							{
								strb.Append("$(");
								strb.Append(str);
								strb.Append(")");
							}

							strb.Append('"');
						}

						return strb.ToString();
					},
					Optimize = a => null
				};
			}
			else if (name == $"op_{CompilerOperators.Not}" && parameters.Length == 1 && parameters[0] == CompilerDefined.Boolean)
			{
				return new CompilerMethod
				{
					Name = name,
					Parameters = parameters.Select((x, i) => new ParameterBind { Name = $"p{i}", Type = x }).ToList(),
					ReturnType = CompilerDefined.Boolean,
					Generate = strs => $"!({strs[0]})",
					Optimize = expressions =>
					{
						if (expressions[0] is ConstantVarExpression<bool> @const)
						{
							return new ConstantVarExpression<bool>(!@const.Value);
						}

						return null;
					}
				};
			}
			else if (name == $"op_{CompilerOperators.Equals}" && IsLen(2))
			{
				return Gen
				(
					CompilerDefined.Boolean,
					strs => $"{strs[0]} -eq {strs[1]}"
				);
			}

			return default;

			bool IsLen(int amt) => parameters.Length == amt;

			CompilerMethod Gen(IType returns, Func<List<string>, string> gen, Func<List<VarExpression>, VarExpression?>? optimize = null)
			{
				return new CompilerMethod
				{
					Name = name,
					Parameters = parameters.Select((x, i) => new ParameterBind { Name = $"p{i}", Type = x }).ToList(),
					ReturnType = returns,
					Generate = gen,
					Optimize = optimize ?? (a => null)
				};
			}
		}

		public void Write(IndentedTextWriter writer, VarCodeStore store)
		{
			// we want to write out every method that isn't the entrypoint

			foreach (var function in store.Functions)
			{
				writer.WriteLine($"function {GetName(function.Name)}({Parameters(function.ParameterCount)})");
				writer.WriteLine("{");
				writer.Indent++;

				Write(writer, store, function.Instructions);

				writer.Indent--;
				writer.WriteLine("}");
			}

			// now let's write out the main method

			Write(writer, store, store.Instructions);
		}

		public void Write(IndentedTextWriter writer, VarCodeStore store, List<VarInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				switch (instruction)
				{
					case VarAssignment o:
					{
						writer.WriteLine($"${GetName(o.VariableId)} = {Expression(o.Value)}");
					}
					break;

					case VarReturn o:
					{
						writer.WriteLine($"return {Expression(o.Value)}");
					}
					break;

					case VarMethodCall o:
					{
						writer.WriteLine(Expression(o.MethodCallVarExpression));
					}
					break;

					case VarIf o:
					{
						writer.WriteLine($"if ({Expression(o.ComparisonExpression)}) {{");
						writer.Indent++;

						Write(writer, store, o.TrueBody);

						writer.Indent--;
						writer.WriteLine("}");
					}
					break;
				}
			}

			string Expression(VarExpression expr) => PowershellTarget.Expression(store, expr);
		}

		private static string Expression(VarCodeStore store, VarExpression expression)
				=> expression switch
				{
					ConstantVarExpression<string> o => $"\"{Sanitize(o.Value)}\"",
					ConstantVarExpression<BigInteger> o => $"(New-Object System.Numerics.BigInteger(\"{o.Value.ToString()}\"))",
					ConstantVarExpression<bool> o => o.Value ? "$TRUE" : "$FALSE",
					ReferenceVarExpression o => $"${GetName(o.VariableId)}",
					MethodCallVarExpression o => GetMethodCall(store, o),
					ParameterReferenceVarExpression o => $"$p{o.ParameterId}",
					_ => throw new NotImplementedException(),
				};

		private static string GetMethodCall(VarCodeStore store, MethodCallVarExpression methodCall)
		{
			var strb = new StringBuilder(50);
			strb.Append('(');

			var structure = store.Functions.FirstOrDefault(x => x.Name == methodCall.MethodId);

			if (structure == null)
			{
				// structure is null, let's try to find a corresponding compiler method
				var compilerMethod = store.CompilerMethods[methodCall.MethodId];

				if (compilerMethod == null)
				{
					throw new Exception("huh");
				}

				return compilerMethod.Generate(methodCall.Parameters.Select(Expression).ToList());
			}
			else
			{
				strb.Append(GetName(methodCall.MethodId));

				if (methodCall.Parameters.Count > 0)
				{
					strb.Append(' ');
					strb.Append($"{Expression(methodCall.Parameters[0])}");

					for (int i = 1; i < methodCall.Parameters.Count; i++)
					{
						strb.Append(", ");
						strb.Append($"{Expression(methodCall.Parameters[i])}");
					}
				}
			}

			strb.Append(')');
			return strb.ToString();

			string Expression(VarExpression expr) => PowershellTarget.Expression(store, expr);
		}

		private static string Sanitize(string str)
		{
			var strb = new StringBuilder(str.Length);

			foreach (var c in str)
			{
				if (c == '\n')
				{
					strb.Append('`');
					strb.Append('n');
				}
				else if (c == '\t')
				{
					strb.Append('`');
					strb.Append('t');
				}
				else if (c == '`')
				{
					strb.Append('`');
					strb.Append('`');
				}
				else
				{
					strb.Append(c);
				}
			}

			return strb.ToString();
		}

		private static string Parameters(int parameters)
		{
			if (parameters == 0) return "";

			var strb = new StringBuilder("$p0");

			for (var i = 1; i < parameters; i++)
			{
				strb.Append(',');
				strb.Append($"$p{i}");
			}

			return strb.ToString();
		}
		*/
	}
}