using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using Terumi.Binder;

namespace Terumi.Targets
{
	public interface ICompilerTarget
	{
		CompilerMethod? Match(string name, List<Expression> arguments) => Match(name, arguments.Select(x => x.Type).ToArray());
		CompilerMethod? Match(string name, params IType[] types);
		CompilerMethod Panic(IType claimToReturn);

		void Write(IndentedTextWriter writer, List<VarCode.Method> methods, int objectFields) => Write(writer, methods);

		[Obsolete("Please specify the amount of object fields")]
		void Write(IndentedTextWriter writer, List<VarCode.Method> methods);

		string ShellFileName { get; }
	}
}