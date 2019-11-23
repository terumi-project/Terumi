using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Terumi.Binder;

namespace Terumi.Targets
{
	public interface ICompilerTarget
	{
		CompilerMethod? Match(string name, List<Expression> arguments);

		void Write(IndentedTextWriter writer, List<VarCode.InstructionMethod> methods);
	}
}