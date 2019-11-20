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

		public SourceFile(ConsumedTokens consumed, PackageLevel packageLevel, List<PackageLevel> usings)
		{
			Consumed = consumed;
			PackageLevel = packageLevel;
			Usings = usings;
		}
	}
}
