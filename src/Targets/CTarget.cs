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
				case TargetMethodNames.Command: return Helper(BuiltinType.Void, "cc_command", types);
				case TargetMethodNames.Panic: return Helper(BuiltinType.Void, "cc_panic", types);
				case TargetMethodNames.Println: return Helper(BuiltinType.Void, "cc_println", types);
				default: return null;
			}
		}

		public CompilerMethod Panic(IType claimToReturn)
		{
			return Helper(claimToReturn, "cc_panic", BuiltinType.String);
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

		private static string GetName(int id)
			=> $"__terumi_{id}";
	}
}
