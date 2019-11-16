using System.CodeDom.Compiler;
using System.Collections.Generic;
using Terumi.Binder;
using Terumi.VarCode.Optimizer.Omega;

namespace Terumi.Targets
{
	public interface ICompilerTarget
	{
		CompilerMethod? MatchMethod(string name, params IType[] parameters);

		void Write(IndentedTextWriter writer, VarCodeStore store);
	}
}