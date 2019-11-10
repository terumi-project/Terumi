using System.Collections.Generic;

namespace Terumi.SyntaxTree
{
	public class CompilerUnit
	{
		public CompilerUnit(List<CompilerUnitItem> compilerUnitItem)
			=> CompilerUnitItems = compilerUnitItem;

		public List<CompilerUnitItem> CompilerUnitItems { get; }
	}
}