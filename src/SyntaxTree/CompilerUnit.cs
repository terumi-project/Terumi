using System.Collections.Generic;

namespace Terumi.SyntaxTree
{
	public class CompilerUnit
	{
		public CompilerUnit(List<CompilerUnitItem> items)
			: this (items.ToArray())
		{
		}

		public CompilerUnit(CompilerUnitItem[] compilerUnitItem)
			=> CompilerUnitItems = compilerUnitItem;

		public CompilerUnitItem[] CompilerUnitItems { get; }
	}
}