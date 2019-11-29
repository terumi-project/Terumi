using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Terumi.Binder
{
	public class BoundFile
	{
		private class InternalFilePathComparer : IEqualityComparer<BoundFile>
		{
			public bool Equals([AllowNull] BoundFile x, [AllowNull] BoundFile y)
				=> x?.FilePath == y?.FilePath;

			public int GetHashCode([DisallowNull] BoundFile obj) => obj.FilePath.GetHashCode();
		}

		public static IEqualityComparer<BoundFile> FilePathComparer { get; } = new InternalFilePathComparer();

		public BoundFile(string filePath, PackageLevel @namespace, List<PackageLevel> usings, List<Method> methods, List<Class> classes)
		{
			Namespace = @namespace;
			Usings = usings;
			Methods = methods;
			Classes = classes;
			FilePath = filePath;
		}

		public PackageLevel Namespace { get; }
		public List<PackageLevel> Usings { get; }

		public List<Method> Methods { get; }
		public List<Class> Classes { get; }

		// we use the path to the file for equality
		public string FilePath { get; }
	}
}
