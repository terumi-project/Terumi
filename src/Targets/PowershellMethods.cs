using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Terumi.Ast;
using Terumi.Binder;
using Terumi.VarCode;
using Terumi.VarCode.Optimizer.Omega;

namespace Terumi.Targets
{
	public class PowershellTarget : ICompilerTarget
	{
		public CompilerMethod? MatchMethod(string name, params IType[] parameters)
		{
			if (name == "println" && parameters.Length > 0)
			{
				return new CompilerMethod
				{
					Name = name,
					Parameters = parameters.Select((x, i) => new ParameterBind { Name = $"p{i}", Type = x }).ToList(),
					ReturnType = CompilerDefined.Void,
					Generate = strs => $"Write-Host \"{(strs.Count == 1 ? $"$({strs[0]})" : strs.Aggregate((a, b) => $"$({a})$({b})"))}\""
				};
			}

			return default;
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

		private static string GetName(VarCodeId id)
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