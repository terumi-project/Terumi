using System.Collections.Generic;

using Terumi.SyntaxTree;

namespace Terumi.Workspace
{
	public class ParsedProjectFile
	{
		public ParsedProjectFile
		(
			string[] @namespace,
			List<PackageLevel> usings,
			List<TypeDefinition> typeDefinitions,
			List<Method> methods
		)
		{
			Namespace = @namespace;
			Usings = usings;
			TypeDefinitions = typeDefinitions;
			Methods = methods;
		}

		public string[] Namespace { get; }
		public List<PackageLevel> Usings { get; }
		public List<TypeDefinition> TypeDefinitions { get; }
		public List<Method> Methods { get; }
	}
}