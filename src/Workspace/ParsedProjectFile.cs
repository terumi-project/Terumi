using System.Collections.Generic;

namespace Terumi.Workspace
{
	public class ParsedProjectFile
	{
		public ParsedProjectFile
		(
			string[] @namespace,
			List<PackageLevel> usings
		)
		{
			Namespace = @namespace;
			Usings = usings;
		}

		public string[] Namespace { get; }
		public List<PackageLevel> Usings { get; }
	}
}