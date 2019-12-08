using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Terumi.Binder;

namespace Terumi.Targets
{
	public class CTarget : ICompilerTarget
	{
		public string ShellFileName => "out.c";

		public CompilerMethod Match(string name, params IType[] types)
		{
			switch (name)
			{
				case TargetMethodNames.Command: return C(BuiltinType.Void);
				case TargetMethodNames.Panic: return C(BuiltinType.Void);
				case TargetMethodNames.Println: return C(BuiltinType.Void);
				case TargetMethodNames.IsSupported: return C(BuiltinType.Boolean);

				case TargetMethodNames.OperatorAnd: return C(BuiltinType.Boolean);
				case TargetMethodNames.OperatorOr: return C(BuiltinType.Boolean);
				case TargetMethodNames.OperatorNot: return C(BuiltinType.Boolean);

				case TargetMethodNames.OperatorEqualTo: return C(BuiltinType.Boolean);
				case TargetMethodNames.OperatorNotEqualTo: return C(BuiltinType.Boolean);

				case TargetMethodNames.OperatorLessThan: return C(BuiltinType.Boolean);
				case TargetMethodNames.OperatorGreaterThan: return C(BuiltinType.Boolean);
				case TargetMethodNames.OperatorLessThanOrEqualTo: return C(BuiltinType.Boolean);
				case TargetMethodNames.OperatorGreaterThanOrEqualTo: return C(BuiltinType.Boolean);

				case TargetMethodNames.OperatorAdd: return C(types[0]);
				case TargetMethodNames.OperatorNegate: return C(types[0]);
				case TargetMethodNames.OperatorSubtract: return C(types[0]);
				case TargetMethodNames.OperatorMultiply: return C(types[0]);
				case TargetMethodNames.OperatorDivide: return C(types[0]);
				case TargetMethodNames.OperatorExponent: return C(types[0]);
				default: return null;
			}

			CompilerMethod C(IType type) => Helper(type, name, types);
		}

		public CompilerMethod Panic(IType claimToReturn)
		{
			return Helper(claimToReturn, "panic", BuiltinType.String);
		}

		private CompilerMethod Helper(IType returns, string name, params IType[] types)
		{
			return new CompilerMethod(returns, name, types.Select((x, i) => new MethodParameter(x, $"p_{i}")).ToList());
		}

		public void Write(IndentedTextWriter writer, List<VarCode.Method> methods)
		{
			using var sr = new StreamReader(typeof(CTarget).Assembly.GetManifestResourceStream("Terumi.c_target.c"));

			var run = new List<string>();

			while (!sr.EndOfStream)
			{
				var line = sr.ReadLine();
				if (line == null) continue;

				if (line == "// <INJECT__CODE>")
				{
					foreach (var method in methods)
					{
						if (method.Name.EndsWith("##main") && method.Parameters.Count == 0) run.Add(GetName(method.Id));
						writer.WriteLine($"// {method.Name}");

						Translate(writer, method);
					}
				}
				else if (line == "// <INJECT__RUN>")
				{
					foreach(var method in run)
					{
						writer.WriteLine($"{method}(0);");
					}
				}
				else
				{
					writer.WriteLine(line);
				}
			}
		}

		public void Translate(IndentedTextWriter writer, VarCode.Method method)
		{
			// check if the method returns anything
			if (method.Returns == ObjectType.Void)
			{
				writer.Write("void ");
			}
			else
			{
				writer.Write("struct Value* ");
			}

			writer.Write(GetName(method.Id));
			writer.WriteLine("(struct Value* parameters) {");
			writer.Indent++;

			Translate(writer, new List<int>(), method.Code);

			writer.Indent--;
			writer.WriteLine('}');
			writer.WriteLine();
		}

		public void Translate(IndentedTextWriter writer, List<int> decl, List<VarCode.Instruction> instruction)
		{
			int index = 0;
			foreach (var i in instruction)
			{
				writer.WriteLine($"// instruction '{index}': {i}");

				switch (i)
				{
					case VarCode.Instruction.Load.String o:
						writer.WriteLine($"struct Value* {GetVarName(o.Store)} = load_string(\"{o.Value}\");");
						break;

					case VarCode.Instruction.Load.Number o:
						writer.WriteLine($"struct Value* {GetVarName(o.Store)} = load_number(\"{o.Value}\");");
						break;

					case VarCode.Instruction.Load.Boolean o:
						writer.WriteLine($"struct Value* {GetVarName(o.Store)} = load_number(\"{(o.Value ? "TRUE" : "FALSE")}\");");
						break;

					case VarCode.Instruction.Assign o:
						EnsureVarExists(o.Value);
						writer.WriteLine($"assign({GetVarName(o.Store)}, {GetVarName(o.Value)});");
						break;

					case VarCode.Instruction.Call o:
					{
						writer.WriteLine(@$"struct Value* __tmp_parameters = malloc_values({o.Arguments.Count});");
						int argIndex = 0;
						foreach (var p in o.Arguments)
						{
							writer.WriteLine($"__tmp_parameters[{i}] = *{GetVarName(p)};");
							argIndex++;
						}

						if (o.Store != -1)
						{
							EnsureVarExists(o.Store);
							writer.Write($"{GetVarName(o.Store)} = ");
						}

						writer.WriteLine($"{GetName(o.Method.Id)}(__tmp_parameters);");
					}
					break;

					case VarCode.Instruction.CompilerCall o:
					{
						// TODO: support +1 args
						// TODO: support return types
						if (o.CompilerMethod == null)
						{
							writer.WriteLine($"cc_panic(\"couldn't find a matching compiler call.\");");
							break;
						}

						if (o.Store != -1)
						{
							EnsureVarExists(o.Store);
							writer.Write($"{GetVarName(o.Store)} = ");
						}

						writer.Write($"cc_{o.CompilerMethod.Name}({GetVarName(o.Arguments[0])}");

						for (int i2 = 1; i2 < o.Arguments.Count; i2++)
						{
							writer.Write($", {GetVarName(o.Arguments[i2])}");
						}

						writer.WriteLine(");");
					}
					break;

					case VarCode.Instruction.SetField o:
					{
						writer.WriteLine($"set_field({GetVarName(o.VariableId)}, {o.FieldId}, {GetVarName(o.ValueId)});");
					}
					break;

					case VarCode.Instruction.GetField o:
					{
						EnsureVarExists(o.StoreId);
						writer.WriteLine($"{GetVarName(o.StoreId)} =  get_field({GetVarName(o.VariableId)}, {o.FieldId})");
					}
					break;

					case VarCode.Instruction.New o:
					{
						EnsureVarExists(o.StoreId);
						writer.WriteLine($"{GetVarName(o.StoreId)} = new_object();");
					}
					break;

					case VarCode.Instruction.Return o:
					{
						if (o.ValueId == -1)
						{
							writer.WriteLine("return;");
						}

						writer.WriteLine($"return {GetVarName(o.ValueId)};");
					}
					break;

					case VarCode.Instruction.If o:
					{
						writer.WriteLine($"if (do_comparison({GetVarName(o.Variable)})) {{");
						writer.Indent++;

						var declBackup = decl.ToArray();
						Translate(writer, decl, o.Clause);
						decl = new List<int>(declBackup);

						writer.Indent--;
						writer.WriteLine('}');
					}
					break;

					case VarCode.Instruction.While o:
					{
						writer.WriteLine($"while (do_comparison({GetVarName(o.Comparison)})) {{");
						writer.Indent++;

						var declBackup = decl.ToArray();
						Translate(writer, decl, o.Clause);
						decl = new List<int>(declBackup);

						writer.Indent--;
						writer.WriteLine('}');
					}
					break;
				}

				index++;
			}

			void EnsureVarExists(int ensure) => this.EnsureVarExists(writer, ensure, decl);
		}

		private void EnsureVarExists(IndentedTextWriter writer, int ensure, List<int> decl)
		{
			if (!decl.Contains(ensure))
			{
				writer.WriteLine($"struct Value* {GetVarName(ensure)} = malloc_value();");
				decl.Add(ensure);
			}
		}

		private static string GetVarName(int id)
			=> $"v_{id}";

		private static string GetName(int id)
			=> $"__terumi_{id}";
	}
}
