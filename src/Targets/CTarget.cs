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
		public static int call_id = 0;
		public string ShellFileName => "out.c";

		public CompilerMethod Match(string name, params IType[] types)
		{
			switch (name)
			{
				case TargetMethodNames.TargetName: return C(BuiltinType.String);
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
			=> Write(writer, methods, 3);

		public void Write(IndentedTextWriter writer, List<VarCode.Method> methods, int objectFields)
		{
			using var sr = new StreamReader(typeof(CTarget).Assembly.GetManifestResourceStream("Terumi.c_target.c"));

			var run = new List<string>();

			while (!sr.EndOfStream)
			{
				var line = sr.ReadLine();
				if (line == null) continue;

				if (line == "#define GC_OBJECT_FIELDS 3")
				{
					writer.WriteLine("#define GC_OBJECT_FIELDS " + objectFields);
				}
				else if (line == "// <INJECT__CODE>")
				{
					// create every function prototype
					foreach (var method in methods)
					{
						writer.WriteLine($"// {method.Name}");
						WriteHeader(writer, method);
						writer.WriteLine(';');
						writer.WriteLine();
					}

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

		public void WriteHeader(IndentedTextWriter writer, VarCode.Method method)
		{
			// check if the method returns anything
			if (method.Returns == ObjectType.Void)
			{
				writer.Write("void ");
			}
			else
			{
				writer.Write("struct GCEntry* ");
			}

			writer.Write(GetName(method.Id));
			writer.Write($"(struct GCEntry* parameters[{method.Parameters.Count}])");
		}

		public void Translate(IndentedTextWriter writer, VarCode.Method method)
		{
			WriteHeader(writer, method);
			writer.WriteLine(" {");
			writer.Indent++;

			writer.WriteLine($"TRACE(\"{GetName(method.Id)}\");");
			_hacky_workaround_method = method;
			Translate(writer, new List<int>(), method.Code);
			writer.WriteLine($"TRACE_EXIT(\"{GetName(method.Id)}\");");

			writer.Indent--;
			writer.WriteLine('}');
			writer.WriteLine();
		}

		private VarCode.Method _hacky_workaround_method;
		public void Translate(IndentedTextWriter writer, List<int> decl, List<VarCode.Instruction> instruction)
		{
			int index = 0;
			foreach (var i in instruction)
			{
				writer.WriteLine($"// instruction '{index}': {i}");

				switch (i)
				{
					case VarCode.Instruction.Load.String o:
						EnsureVarExists(o.Store);
						writer.WriteLine($"{GetVarName(o.Store)} = gc_handhold(instruction_load_string(\"{o.Value}\"));");
						break;

					case VarCode.Instruction.Load.Number o:
						EnsureVarExists(o.Store);
						writer.WriteLine($"{GetVarName(o.Store)} = gc_handhold(instruction_load_number({o.Value.Value}));");
						break;

					case VarCode.Instruction.Load.Boolean o:
						EnsureVarExists(o.Store);
						writer.WriteLine($"{GetVarName(o.Store)} = gc_handhold(instruction_load_boolean({(o.Value ? "true" : "false")}));");
						break;

					case VarCode.Instruction.Load.Parameter o:
						EnsureVarExists(o.Store);
						writer.WriteLine($"{GetVarName(o.Store)} = parameters[{o.ParameterNumber}];");
						break;

					case VarCode.Instruction.Assign o:
						EnsureVarExists(o.Store, true);
						writer.WriteLine($"instruction_assign({GetVarName(o.Store)}->value, {GetVarName(o.Value)}->value);");
						break;

					case VarCode.Instruction.Call o:
					{
						string call = $"__call_{GetName(o.Method.Id)}_{call_id++}";

						writer.WriteLine(@$"struct GCEntry* {call}[{o.Arguments.Count}];");
						int argIndex = 0;
						foreach (var p in o.Arguments)
						{
							writer.WriteLine($"{call}[{argIndex}] = {GetVarName(p)};");
							argIndex++;
						}

						if (o.Store != -1)
						{
							EnsureVarExists(o.Store);
							writer.Write($"{GetVarName(o.Store)} = ");
						}

						writer.WriteLine($"{GetName(o.Method.Id)}({call});");
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
							writer.Write($"{GetVarName(o.Store)} = gc_handhold(");
						}

						writer.Write($"cc_{o.CompilerMethod.Name}(");

						if (o.Arguments.Count > 0)
						{
							writer.Write($"{GetVarName(o.Arguments[0])}->value");

							for (int i2 = 1; i2 < o.Arguments.Count; i2++)
							{
								writer.Write($", {GetVarName(o.Arguments[i2])}->value");
							}
						}

						if (o.Store != -1)
						{
							writer.Write(")");
						}

						writer.WriteLine(");");
					}
					break;

					case VarCode.Instruction.SetField o:
					{
						writer.WriteLine($"instruction_set_field({GetVarName(o.ValueId)}, {GetVarName(o.VariableId)}->value, {o.FieldId});");
					}
					break;

					case VarCode.Instruction.GetField o:
					{
						EnsureVarExists(o.StoreId);
						writer.WriteLine($"{GetVarName(o.StoreId)} = instruction_get_field({GetVarName(o.VariableId)}->value, {o.FieldId});");
					}
					break;

					case VarCode.Instruction.New o:
					{
						EnsureVarExists(o.StoreId);
						writer.WriteLine($"{GetVarName(o.StoreId)} = gc_handhold(instruction_new());");
					}
					break;

					case VarCode.Instruction.Return o:
					{
						writer.WriteLine($"TRACE_EXIT(\"{GetName(_hacky_workaround_method.Id)}\");");

						if (o.ValueId == -1)
						{
							writer.WriteLine("return;");
						}

						writer.WriteLine($"return {GetVarName(o.ValueId)};");
					}
					break;

					case VarCode.Instruction.If o:
					{
						writer.WriteLine($"if (value_unpack_boolean({GetVarName(o.Variable)}->value)) {{");
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
						writer.WriteLine($"while (value_unpack_boolean({GetVarName(o.Comparison)}->value)) {{");
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

			void EnsureVarExists(int ensure, bool alloc = false) => this.EnsureVarExists(writer, ensure, decl, alloc);
		}

		private void EnsureVarExists(IndentedTextWriter writer, int ensure, List<int> decl, bool alloc)
		{
			if (!decl.Contains(ensure))
			{
				writer.WriteLine($"struct GCEntry* {GetVarName(ensure)};");
				decl.Add(ensure);

				if (alloc)
				{
					writer.WriteLine($"{GetVarName(ensure)} = gc_handhold(value_blank(UNKNOWN));");
				}
			}
		}

		private static string GetVarName(int id)
			=> $"v_{id}";

		private static string GetName(int id)
			=> $"__terumi_{id}";
	}
}
