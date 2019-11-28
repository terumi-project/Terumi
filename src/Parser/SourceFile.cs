using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.Parser
{
	public class SourceFile
	{
		public ConsumedTokens Consumed { get; }
		public PackageLevel PackageLevel { get; }
		public List<PackageLevel> Usings { get; }
		public List<Method> Methods { get; }
		public List<Class> Classes { get; }

		public List<PackageLevel> UsingsWithSelf { get; }

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

			UsingsWithSelf = Usings.Append(PackageLevel).Distinct().ToList();
		}
	}
}
