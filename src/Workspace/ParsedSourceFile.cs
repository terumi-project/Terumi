using System.Collections.Generic;

using Terumi.SyntaxTree;

namespace Terumi.Workspace
{
	public class ParsedSourceFile
	{
		public ParsedSourceFile
		(
			PackageLevel @namespace,
			IReadOnlyCollection<PackageLevel> usings,
			IReadOnlyCollection<TypeDefinition> typeDefinitions
		)
		{
			Namespace = @namespace;
			Usings = usings;
			TypeDefinitions = typeDefinitions;
		}

		public PackageLevel Namespace { get; }
		public IReadOnlyCollection<PackageLevel> Usings { get; }
		public IReadOnlyCollection<TypeDefinition> TypeDefinitions { get; }
	}
}