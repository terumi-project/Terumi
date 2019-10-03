using System.Collections.Generic;

namespace Terumi.Ast
{
	public class CompilationUnit
	{
		public CompilationUnit(IReadOnlyCollection<CompilationNode> nodes)
		{
			Nodes = nodes;
		}

		public IReadOnlyCollection<CompilationNode> Nodes { get; }
	}
}