﻿using System;
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

				case TargetMethodNames.StringSubstring: return C(types[0]);
				case TargetMethodNames.StringLength: return C(BuiltinType.Number);

				case TargetMethodNames.FilesystemCurrentPath: return C(BuiltinType.String);
				case TargetMethodNames.FilesystemVulnerableRead: return C(BuiltinType.String);
				case TargetMethodNames.FilesystemVulnerableEntryCount: return C(BuiltinType.Number);
				case TargetMethodNames.FilesystemVulnerableEntryRead: return C(BuiltinType.String);

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
						if (method.IsEntryPoint) run.Add(GetName(method.Id));
						writer.WriteLine($"// {method.Name}");

						Translate(writer, method);
					}
				}
				else if (line == "// <INJECT__RUN>")
				{
					foreach(var method in run)
					{
						writer.WriteLine($"{method}(&command_line);");
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

			var decl = new List<int>();
			var pcalls = new List<int>();
			Translate(writer, decl, pcalls, method.Code);

			// when we return from a method we must mark everything
			// that was created as not active so the GC can free it
			foreach (var variable in decl)
			{
				writer.WriteLine($"{GetVarName(variable)}->active = false;");
			}

			// anything passed in as a parameter should remain active
			foreach (var p in pcalls)
			{
				writer.WriteLine($"{GetVarName(p)}->active = true;");
			}

			writer.WriteLine($"TRACE_EXIT(\"{GetName(method.Id)}\");");

			writer.Indent--;
			writer.WriteLine('}');
			writer.WriteLine();
		}

		private VarCode.Method _hacky_workaround_method;
		public void Translate(IndentedTextWriter writer, List<int> decl, List<int> pcalls, List<VarCode.Instruction> instruction)
		{
			int index = 0;
			foreach (var i in instruction)
			{
				writer.WriteLine($"// instruction '{index}': {i}");

				switch (i)
				{
					case VarCode.Instruction.Load.String o:
						EnsureVarExists(o.Store);
						writer.WriteLine($"{GetVarName(o.Store)} = gc_handhold(instruction_load_string(\"{Escape(o.Value)}\"));");
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
						pcalls.Add(o.Store);
						writer.WriteLine($"{GetVarName(o.Store)} = parameters[{o.ParameterNumber}];");
						break;

					case VarCode.Instruction.Assign o:
						EnsureVarExists(o.Store, true, dontMarkInactive: true);
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
						EnsureVarExists(o.Store);
						writer.WriteLine($"{GetVarName(o.Store)} = instruction_get_field({GetVarName(o.VariableId)}->value, {o.FieldId});");
					}
					break;

					case VarCode.Instruction.New o:
					{
						EnsureVarExists(o.Store);
						writer.WriteLine($"{GetVarName(o.Store)} = gc_handhold(instruction_new());");
					}
					break;

					case VarCode.Instruction.Return o:
					{
						// when we return from a method we must mark everything
						// that was created as not active so the GC can free it
						foreach (var variable in decl)
						{
							writer.WriteLine($"{GetVarName(variable)}->active = false;");
						}

						// anything passed in as a parameter should remain active
						foreach (var p in pcalls)
						{
							writer.WriteLine($"{GetVarName(p)}->active = true;");
						}

						writer.WriteLine($"TRACE_EXIT(\"{GetName(_hacky_workaround_method.Id)}\");");

						if (o.ValueId == -1)
						{
							writer.WriteLine("return;");
							break;
						}

						writer.WriteLine($"{GetVarName(o.ValueId)}->active = true;");
						writer.WriteLine($"return {GetVarName(o.ValueId)};");
					}
					break;

					case VarCode.Instruction.If o:
					{
						writer.WriteLine($"if (value_unpack_boolean({GetVarName(o.ComparisonId)}->value)) {{");
						writer.Indent++;

						var childDecl = new List<int>(decl);
						var pcalls2 = new List<int>();
						Translate(writer, childDecl, pcalls2, o.Clause);

						// foreach declaration that isn't in the parent scope,
						// we need to mark that GCEntry as not active so the GC
						// can free it
						foreach (var variable in childDecl.Where(x => !decl.Contains(x)))
						{
							writer.WriteLine($"{GetVarName(variable)}->active = false;");
						}

						// anything passed in as a parameter should remain active
						foreach (var p in pcalls2)
						{
							writer.WriteLine($"{GetVarName(p)}->active = true;");
						}

						writer.Indent--;
						writer.WriteLine('}');
					}
					break;

					case VarCode.Instruction.While o:
					{
						writer.WriteLine($"while (value_unpack_boolean({GetVarName(o.ComparisonId)}->value)) {{");
						writer.Indent++;

						var childDecl = new List<int>(decl);
						var pcalls2 = new List<int>();
						Translate(writer, childDecl, pcalls2, o.Clause);

						// foreach declaration that isn't in the parent scope,
						// we need to mark that GCEntry as not active so the GC
						// can free it
						foreach (var variable in childDecl.Where(x => !decl.Contains(x)))
						{
							writer.WriteLine($"{GetVarName(variable)}->active = false;");
						}

						// anything passed in as a parameter should remain active
						foreach (var p in pcalls2)
						{
							writer.WriteLine($"{GetVarName(p)}->active = true;");
						}

						writer.Indent--;
						writer.WriteLine('}');
					}
					break;
				}

				index++;
			}

			void EnsureVarExists(int ensure, bool alloc = false, bool dontMarkInactive = false) => this.EnsureVarExists(writer, ensure, decl, alloc, dontMarkInactive);
		}

		private void EnsureVarExists(IndentedTextWriter writer, int ensure, List<int> decl, bool alloc, bool dontMarkInactive)
		{
			var allocated = false;

			if (!decl.Contains(ensure))
			{
				writer.WriteLine($"struct GCEntry* {GetVarName(ensure)};");
				decl.Add(ensure);

				if (alloc)
				{
					writer.WriteLine($"{GetVarName(ensure)} = gc_handhold(value_blank(OBJ_UNKNOWN));");
					allocated = true;
				}
			}
			else if (!allocated)
			{
				// probably about to set it to a new value
				// tell the GC that we don't want to keep track of this one

				// if we use instruction_assign we're not going to change the GCEntry, just the value
				if (!dontMarkInactive)
				{
					writer.WriteLine($"{GetVarName(ensure)}->active = false;");
				}
			}
		}

		private static string GetVarName(int id)
			=> $"v_{id}";

		private static string GetName(int id)
			=> $"__terumi_{id}";

		public static string Escape(string input)
		{
			var strb = new StringBuilder(input.Length + 10);

			foreach (var i in input.Replace("\r\n", "\n").Replace('\r', '\n'))
			{
				switch (i)
				{
					case '"': strb.Append('\\').Append('"'); break;
					case '\n': strb.Append('\\').Append('n'); break;
					case '\t': strb.Append('\\').Append('t'); break;
					default: strb.Append(i); break;
				}
			}

			return strb.ToString();
		}
	}
}
