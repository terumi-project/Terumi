namespace Terumi.SyntaxTree
{
	public class CompilerUnit
	{
		public CompilerUnit(CompilerUnitItem[] compilerUnitItem)
		{
			CompilerUnitItems = compilerUnitItem;
		}

		public CompilerUnitItem[] CompilerUnitItems { get; }
	}
}