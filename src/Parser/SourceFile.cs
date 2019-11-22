using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Parser
{
	public class SourceFile
	{
		public static SourceFile Empty { get; }

		public ConsumedTokens Consumed { get; }
		public PackageLevel PackageLevel { get; }
		public List<PackageLevel> Usings { get; }
		public List<Method> Methods { get; }
		public List<Class> Classes { get; }

		public SourceFile
		(
			ConsumedTokens consumed,
			PackageLevel packageLevel,
			List<PackageLevel>? usings = null,
			List<Method>? methods = null,
			List<Class>? classes = null
		)
		{
			Consumed = consumed;
			PackageLevel = packageLevel;
			Usings = usings ?? EmptyList<PackageLevel>.Instance;
			Methods = methods ?? EmptyList<Method>.Instance;
			Classes = classes ?? EmptyList<Class>.Instance;
		}
	}
}
