using System.Collections.Generic;
using System.Linq;
using Terumi.Ast;
using Terumi.SyntaxTree;

namespace Terumi.Binder
{
	public class PromisedCompilationNode : CompilationNode, ICompilationType
	{
		public PromisedCompilationNode
		(
			TypeDefinition typeDefinition,
			PackageLevel @namespace,
			PackageLevel[] usings
		)
		{
			TypeDefinition = typeDefinition;
			Namespace = new Namespace(new List<string>(@namespace.Levels).AsReadOnly());
			Usings = usings.Select(x => new Namespace(new List<string>(x.Levels).AsReadOnly())).ToArray();
		}

		public CompilationNode RealNode { get; set; }

		public TypeDefinition TypeDefinition { get; }
		public Namespace Namespace { get; }
		public Namespace[] Usings { get; }

		public string CompilationTypeName => TypeDefinition.Identifier;
	}
}