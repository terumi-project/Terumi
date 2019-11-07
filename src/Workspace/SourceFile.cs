using System;
using System.IO;

using Terumi.SyntaxTree;

namespace Terumi.Workspace
{
	public class SourceFile
	{
		public SourceFile(Stream source, PackageLevel packageLevel, string location)
		{
			Location = location;
			Source = source;
			PackageLevel = packageLevel;
		}

		public string Location { get; }
		public Stream Source { get; }
		public PackageLevel PackageLevel { get; }
	}
}