using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Binder
{
	public class BoundFile
	{
		public BoundFile(PackageLevel @namespace, List<PackageLevel> usings, List<Method> methods, List<Class> classes)
		{
			Namespace = @namespace;
			Usings = usings;
			Methods = methods;
			Classes = classes;
		}

		public PackageLevel Namespace { get; }
		public List<PackageLevel> Usings { get; }

		public List<Method> Methods { get; }
		public List<Class> Classes { get; }
	}
}
