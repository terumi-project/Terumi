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
	public class BashTarget : ICompilerTarget
	{
		public string ShellFileName => "out.sh";

		public CompilerMethod? Match(string name, params IType[] types)
		{
			switch (name)
			{
				// TODO: supply code
				case TargetMethodNames.TargetName: return ReturnMethod(BuiltinType.String, _ => "bash");

				case TargetMethodNames.IsSupported: return ReturnMethod(BuiltinType.Boolean, _ => ""); // TODO: exauhstive
				case TargetMethodNames.Println: return ReturnMethod(BuiltinType.Void, a => $"echo {a[0]}");
				case TargetMethodNames.Command: return ReturnMethod(BuiltinType.Void, a => a[0]);

				case TargetMethodNames.OperatorNot: return ReturnMethod(BuiltinType.Boolean, a => $"if [[ {a[0]} -eq 1 ]]; then ret=0; else ret=1; fi");

				case TargetMethodNames.OperatorEqualTo: return ReturnMethod(BuiltinType.Boolean, a => $"if [[ {a[0]} -eq {a[1]} ]]; then ret=1; else ret=0; fi");
				case TargetMethodNames.OperatorNotEqualTo: return ReturnMethod(BuiltinType.Boolean, a => $"if [[ {a[0]} -ne {a[1]} ]]; then ret=1; else ret=0; fi");

				case TargetMethodNames.OperatorLessThan: return ReturnMethod(BuiltinType.Boolean, a => $"if [[ {a[0]} -lt {a[1]} ]]; then ret=1; else ret=0; fi");
				case TargetMethodNames.OperatorGreaterThan: return ReturnMethod(BuiltinType.Boolean, a => $"if [[ {a[0]} -gt {a[1]} ]]; then ret=1; else ret=0; fi");
				case TargetMethodNames.OperatorLessThanOrEqualTo: return ReturnMethod(BuiltinType.Boolean, a => $"if [[ {a[0]} -le {a[1]} ]]; then ret=1; else ret=0; fi");
				case TargetMethodNames.OperatorGreaterThanOrEqualTo: return ReturnMethod(BuiltinType.Boolean, a => $"if [[ {a[0]} -ge {a[1]} ]]; then ret=1; else ret=0; fi");

				// TODO: verify that both operands are the same
				case TargetMethodNames.OperatorAdd:
				{
					if (types[0] == BuiltinType.Number)
					{
						return ReturnMethod(types[0], a => $"ret=$(({a[0]} + {a[1]}))");
					}
					else
					{
						return ReturnMethod(types[0], a => $"ret=\"{a[0]}\"\"{a[1]}\"");
					}
				}

				case TargetMethodNames.OperatorSubtract: return ReturnMethod(types[0], a => $"ret=$(({a[0]} - {a[1]}))");
				case TargetMethodNames.OperatorMultiply: return ReturnMethod(types[0], a => $"ret=$(({a[0]} * {a[1]}))");
				case TargetMethodNames.OperatorDivide: return ReturnMethod(types[0], a => $"ret=$(({a[0]} / {a[1]}))");
				case TargetMethodNames.OperatorExponent: return ReturnMethod(types[0], a => $"ret=$(({a[0]} ** {a[1]}))");
			}

			throw new NotImplementedException();
			CompilerMethod ReturnMethod(IType returnType, Func<List<string>, string> codegen) => new CompilerMethod(returnType, name, Match(types))
			{
				CodeGen = codegen
			};
		}

		private List<string> _run = new List<string>();

		private List<MethodParameter> Match(IType[] arguments)
			=> arguments.Select((x, i) => new MethodParameter(x, $"p{i}")).ToList();

		public void Write(IndentedTextWriter writer, List<VarCode.Method> methods)
		{
			int id = 0;
			foreach (var method in methods) method.Id = id++;

			writer.WriteLine($"_gc=a");
			writer.WriteLine($"ret=''");

			foreach (var method in methods)
			{
				writer.WriteLine();
				writer.WriteLine($"function {GetName(method.Id)} {{");

				if (method.Name == "<>main" && method.Parameters.Count == 0) _run.Add(GetName(method.Id));

				writer.Indent++;
				Write(writer, method, method.Parameters.Count);
				writer.Indent--;

				writer.WriteLine('}');
			}

			foreach (var run in _run)
			{
				writer.WriteLine();
				writer.Write(run);
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
					writer.WriteLine($"local {GetName(o.Store)}='{o.Value}'");
				}
				break;

				case Instruction.Load.Parameter o:
				{
					writer.WriteLine($"local {GetName(o.Store)}=${o.ParameterNumber + 1}");
				}
				break;

				case Instruction.Load.Boolean o:
				{
					writer.WriteLine($"local {GetName(o.Store)}=$(({(o.Value ? "1" : "0")}))");
				}
				break;

				case Instruction.Load.Number o:
				{
					writer.WriteLine($"local {GetName(o.Store)}=$(({o.Value.Value.ToString()}))");
				}
				break;

				case Instruction.Assign o:
				{
					writer.WriteLine($"local {GetName(o.Store)}=${GetName(o.Value)}");
				}
				break;

				case Instruction.New o:
				{
					writer.WriteLine($"local {GetName(o.StoreId)}=\"$_gc\"");
					writer.WriteLine($"_gc=\"$_gc\"\"a\"");
				}
				break;

				// TODO: figure this out lol
				case Instruction.SetField o:
				{
					writer.WriteLine($"declare -g \"${GetName(o.VariableId)}\"\"{BashTarget.GetName(o.FieldId)}\"=\"${GetName(o.ValueId)}\"");
				}
				break;

				case Instruction.GetField o:
				{
					writer.WriteLine($"local find=\"${GetName(o.VariableId)}\"\"{BashTarget.GetName(o.FieldId)}\"");
					writer.WriteLine($"local {GetName(o.StoreId)}=${{!find}}");
				}
				break;

				case Instruction.Call o:
				{
					var args = o.Arguments.Count == 0 ? "" : o.Arguments.Select(x => "\"$" + GetName(x) + "\"").Aggregate((a, b) => $"{a} {b}");

					writer.WriteLine($"{BashTarget.GetName(o.Method.Id)} {args}");

					if (o.Store != Instruction.Nowhere)
					{
						writer.WriteLine($"local {GetName(o.Store)}=$ret");
					}
				}
				break;

				case Instruction.CompilerCall o:
				{
					var toNames = o.Arguments.Select(x => "$" + GetName(x)).ToList();

					writer.WriteLine($"{o.CompilerMethod.CodeGen(toNames)}");

					if (o.Store != Instruction.Nowhere)
					{
						writer.WriteLine($"local {GetName(o.Store)}=$ret");
					}
				}
				break;

				case Instruction.Return o:
				{
					if (o.ValueId != Instruction.Nowhere)
					{
						writer.WriteLine($"ret=${GetName(o.ValueId)}");
					}

					writer.WriteLine("return 0");
				}
				break;

				case Instruction.If o:
				{
					writer.WriteLine($"if [[ $((${GetName(o.Variable)})) -eq 1 ]]; then");

					writer.Indent++;
					Write(writer, o.Clause, offset);
					writer.Indent--;

					writer.WriteLine("fi");
				}
				break;

				case Instruction.While o:
				{
					writer.WriteLine($"while [[ $((${GetName(o.Comparison)})) -eq 1 ]]; do");

					writer.Indent++;
					Write(writer, o.Clause, offset);
					writer.Indent--;

					writer.WriteLine("done");
				}
				break;

				default: throw new NotImplementedException();
			}

			string GetName(int id) => BashTarget.GetName(id + offset);
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
	}
}