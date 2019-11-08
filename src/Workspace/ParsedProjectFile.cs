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
			List<TypeDefinition> typeDefinitions
		)
		{
			Namespace = @namespace;
			Usings = usings;
			TypeDefinitions = typeDefinitions;
		}

		public string[] Namespace { get; }
		public List<PackageLevel> Usings { get; }
		public List<TypeDefinition> TypeDefinitions { get; }
	}
}